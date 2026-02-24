using System.Collections;
using Coins;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Level
{
    /// <summary>
    /// Manages level state and completion.
    /// Handles what happens when the player reaches the home.
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        #region Serialized Fields

        [Header("References")]
        [SerializeField]
        private Home m_Home;
        [SerializeField]
        private CoinSpawner m_CoinSpawner;
        [SerializeField]
        private LevelLoader m_LevelLoader;
        [SerializeField]
        private Characters.PlayerController m_PlayerController;

        [Header("Level Completion Settings")]
        [SerializeField]
        private float m_LevelCompleteDelay = 2f;

        [Header("UI Messages")]
        [SerializeField]
        private GameObject m_MessagePanel;
        [SerializeField]
        private Text m_MessageText;
        [SerializeField]
        private float m_DeathMessageDelay = 2f;

        [Header("Level Name Display")]
        [SerializeField]
        private Text m_LevelNameText;

        #endregion

        #region Private Fields

        private bool m_LevelComplete = false;
        private bool m_IsRestarting = false;

        #endregion

        #region Unity Lifecycle Methods

        /// <summary>
        /// Subscribes to events when the manager starts.
        /// </summary>
        private void Start()
        {
            // Hide message panel initially
            if (m_MessagePanel != null)
            {
                m_MessagePanel.SetActive(false);
            }

            // Subscribe to home reached event
            if (m_Home != null)
            {
                m_Home.OnHomeReached += HandleHomeReached;
            }
            else
            {
                Debug.LogWarning("LevelManager: Home reference is not assigned!");
            }

            // Subscribe to player death event
            if (m_PlayerController != null)
            {
                m_PlayerController.OnPlayerDied += HandlePlayerDeath;
            }
            else
            {
                Debug.LogWarning("LevelManager: PlayerController reference not set - death handling disabled!");
            }

            // Subscribe to level loader events to update level name
            if (m_LevelLoader != null)
            {
                // We'll get the level name when it's loaded
                StartCoroutine(UpdateLevelNameOnStart());
            }
        }

        /// <summary>
        /// Coroutine to update the level name after the level has loaded.
        /// </summary>
        private IEnumerator UpdateLevelNameOnStart()
        {
            const int maxFramesToWait = 120;
            int waitedFrames = 0;
            while (waitedFrames < maxFramesToWait && (m_LevelLoader == null || m_LevelLoader.CurrentLevelData == null))
            {
                waitedFrames++;
                yield return null;
            }

            UpdateLevelNameFromCurrentLevelData();
        }

        /// <summary>
        /// Unsubscribes from events to prevent memory leaks.
        /// </summary>
        private void OnDestroy()
        {
            if (m_Home != null)
            {
                m_Home.OnHomeReached -= HandleHomeReached;
            }

            if (m_PlayerController != null)
            {
                m_PlayerController.OnPlayerDied -= HandlePlayerDeath;
            }
        }

        #endregion

        #region Private Event Handlers

        /// <summary>
        /// Event handler for when the player reaches the home.
        /// Shows completion message, stops coin spawning, and loads the next level.
        /// </summary>
        private void HandleHomeReached()
        {
            if (m_LevelComplete)
            {
                return; // Prevent multiple triggers
            }

            m_LevelComplete = true;
            Debug.Log("LevelManager: Level complete!");

            // Make player invulnerable to prevent damage during level complete
            if (m_PlayerController != null)
            {
                m_PlayerController.SetInvulnerable(true);
            }

            // Stop spawning new coins
            if (m_CoinSpawner != null)
            {
                m_CoinSpawner.SetSpawnerState(false);
            }

            // Check if this is the last level
            bool isLastLevel = false;
            if (m_LevelLoader != null)
            {
                isLastLevel = m_LevelLoader.IsLastLevel();
            }

            // Show appropriate completion message
            if (isLastLevel)
            {
                // Last level - show only game complete message
                StartCoroutine(ShowGameCompleteMessage());
            }
            else
            {
                // Not last level - show level complete, then load next
                StartCoroutine(ShowLevelCompleteMessage());
            }
        }

        /// <summary>
        /// Event handler for when the player dies.
        /// Shows death message and restarts the current level.
        /// </summary>
        private void HandlePlayerDeath()
        {
            // Prevent multiple death handlers from running
            if (m_IsRestarting)
            {
                Debug.Log("LevelManager: Already restarting, ignoring duplicate death event");
                return;
            }

            m_IsRestarting = true;
            Debug.Log("LevelManager: Player died! Restarting level...");

            // Stop spawning new coins
            if (m_CoinSpawner != null)
            {
                m_CoinSpawner.SetSpawnerState(false);
            }

            // Show death message and restart level after delay
            StartCoroutine(ShowDeathMessage());
        }

        #endregion

        #region Coroutines

        /// <summary>
        /// Shows the level complete message and loads the next level after a delay.
        /// </summary>
        private IEnumerator ShowLevelCompleteMessage()
        {
            // Show message
            if (m_MessagePanel != null && m_MessageText != null)
            {
                m_MessageText.text = "Level Complete!";
                m_MessagePanel.SetActive(true);
            }

            // Wait for delay
            yield return new WaitForSeconds(m_LevelCompleteDelay);

            // Hide message
            if (m_MessagePanel != null)
            {
                m_MessagePanel.SetActive(false);
            }

            // Load next level
            LoadNextLevel();
        }

        /// <summary>
        /// Shows the death message and restarts the current level after a delay.
        /// </summary>
        private IEnumerator ShowDeathMessage()
        {
            // Show message
            if (m_MessagePanel != null && m_MessageText != null)
            {
                m_MessageText.text = "You Died!\nRestarting Level...";
                m_MessagePanel.SetActive(true);
            }

            // Wait for delay
            yield return new WaitForSeconds(m_DeathMessageDelay);

            // Hide message
            if (m_MessagePanel != null)
            {
                m_MessagePanel.SetActive(false);
            }

            // Restart current level
            RestartLevel();
        }

        /// <summary>
        /// Shows the game complete message when all levels are finished.
        /// </summary>
        private IEnumerator ShowGameCompleteMessage()
        {
            // Show message
            if (m_MessagePanel != null && m_MessageText != null)
            {
                m_MessageText.text = "Congratulations!\nGame Complete!";
                m_MessagePanel.SetActive(true);
            }

            // Keep the message visible (no auto-hide for game complete)
            // Could add logic here to return to main menu, restart from level 1, etc.
            yield return null;
        }

        #endregion

        #region UI Methods

        /// <summary>
        /// Updates the level name display.
        /// </summary>
        /// <param name="levelName">The name of the level to display.</param>
        public void UpdateLevelName(string levelName)
        {
            if (m_LevelNameText != null && !string.IsNullOrEmpty(levelName))
            {
                m_LevelNameText.text = levelName;
                Debug.Log($"LevelManager: Level name updated to '{levelName}'");
            }
        }

        #endregion

        #region Private Helper Methods
        
        private void RestartLevel()
        {
            Debug.Log("LevelManager: Restarting current level...");

            // If using JSON level progression, reload the current level
            if (m_LevelLoader != null)
            {
                m_LevelLoader.ReloadCurrentLevel();

                // Reset flags for new attempt
                m_LevelComplete = false;
                m_IsRestarting = false;

                UpdateLevelNameFromCurrentLevelData();
            }
            else
            {
                // Fallback to scene reload (this will reset all flags automatically)
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }

        /// <summary>
        /// Loads the next level or restarts the current level.
        /// Supports JSON level progression or traditional scene loading.
        /// </summary>
        private void LoadNextLevel()
        {
            // Try JSON level progression first
            if (m_LevelLoader != null)
            {
                bool hasNextLevel = m_LevelLoader.LoadNextLevel();

                if (hasNextLevel)
                {
                    Debug.Log("LevelManager: Loading next level from collection");

                    // Reset level complete flag for next level
                    m_LevelComplete = false;

                    UpdateLevelNameFromCurrentLevelData();
                }
                else
                {
                    // No more levels - show game complete message
                    Debug.Log("LevelManager: No more levels. Game complete!");
                    StartCoroutine(ShowGameCompleteMessage());
                }
            }
        }

        private void UpdateLevelNameFromCurrentLevelData()
        {
            if (m_LevelLoader == null || m_LevelLoader.CurrentLevelData == null)
            {
                return;
            }

            string configuredLevelName = m_LevelLoader.CurrentLevelData.levelName;
            if (string.IsNullOrWhiteSpace(configuredLevelName))
            {
                configuredLevelName = m_LevelLoader.CurrentLevelData.levelId;
            }

            if (!string.IsNullOrWhiteSpace(configuredLevelName))
            {
                UpdateLevelName(configuredLevelName);
            }
        }

        #endregion
    }
}
