using System.Collections.Generic;

namespace Level.Data
{
    /// <summary>
    /// Validation utilities for level DTOs loaded from external JSON.
    /// </summary>
    public static class LevelDataValidation
    {
        /// <summary>
        /// Validates the complete level collection and returns a readable error message on failure.
        /// </summary>
        /// <param name="collection">Collection to validate.</param>
        /// <param name="errorMessage">Validation error when invalid, otherwise an empty string.</param>
        /// <returns>True when collection is valid and safe to consume.</returns>
        public static bool TryValidateCollection(LevelCollectionDto collection, out string errorMessage)
        {
            if (collection == null)
            {
                errorMessage = "Level collection is null.";
                return false;
            }

            if (collection.levels == null || collection.levels.Count == 0)
            {
                errorMessage = "Level collection has no levels.";
                return false;
            }

            for (int levelIndex = 0; levelIndex < collection.levels.Count; levelIndex++)
            {
                if (!TryValidateLevel(collection.levels[levelIndex], levelIndex, out errorMessage))
                {
                    return false;
                }
            }

            errorMessage = string.Empty;
            return true;
        }

        private static bool TryValidateLevel(LevelDataDto levelData, int levelIndex, out string errorMessage)
        {
            if (levelData == null)
            {
                errorMessage = $"Level at index {levelIndex} is null.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(levelData.levelId))
            {
                errorMessage = $"Level at index {levelIndex} has empty levelId.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(levelData.levelName))
            {
                errorMessage = $"Level '{levelData.levelId}' has empty levelName.";
                return false;
            }

            if (levelData.goalCoins <= 0)
            {
                errorMessage = $"Level '{levelData.levelId}' has invalid goalCoins={levelData.goalCoins}.";
                return false;
            }

            if (levelData.maxCoinsOnBoard <= 0)
            {
                errorMessage = $"Level '{levelData.levelId}' has invalid maxCoinsOnBoard={levelData.maxCoinsOnBoard}.";
                return false;
            }

            if (levelData.maxCoinsOnBoard > levelData.goalCoins)
            {
                errorMessage =
                    $"Level '{levelData.levelId}' has maxCoinsOnBoard={levelData.maxCoinsOnBoard} greater than goalCoins={levelData.goalCoins}.";
                return false;
            }

            if (!TryValidatePlayerConfig(levelData.playerConfig, levelData.levelId, out errorMessage))
            {
                return false;
            }

            if (!TryValidateZombieConfigs(levelData.zombies, levelData.levelId, out errorMessage))
            {
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        private static bool TryValidatePlayerConfig(PlayerConfigDto playerConfig, string levelId, out string errorMessage)
        {
            if (playerConfig == null)
            {
                errorMessage = $"Level '{levelId}' is missing playerConfig.";
                return false;
            }

            if (playerConfig.speed <= 0f)
            {
                errorMessage = $"Level '{levelId}' has invalid player speed={playerConfig.speed}.";
                return false;
            }

            if (playerConfig.health <= 0)
            {
                errorMessage = $"Level '{levelId}' has invalid player health={playerConfig.health}.";
                return false;
            }

            if (playerConfig.spawnPosition == null)
            {
                errorMessage = $"Level '{levelId}' playerConfig is missing spawnPosition.";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        private static bool TryValidateZombieConfigs(
            List<ZombieConfigDto> zombieConfigs,
            string levelId,
            out string errorMessage)
        {
            if (zombieConfigs == null)
            {
                errorMessage = $"Level '{levelId}' has null zombies collection.";
                return false;
            }

            for (int zombieIndex = 0; zombieIndex < zombieConfigs.Count; zombieIndex++)
            {
                ZombieConfigDto zombieConfig = zombieConfigs[zombieIndex];
                if (zombieConfig == null)
                {
                    errorMessage = $"Level '{levelId}' zombie at index {zombieIndex} is null.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(zombieConfig.zombieId))
                {
                    errorMessage = $"Level '{levelId}' zombie at index {zombieIndex} has empty zombieId.";
                    return false;
                }

                if (zombieConfig.spawnPosition == null)
                {
                    errorMessage = $"Level '{levelId}' zombie '{zombieConfig.zombieId}' is missing spawnPosition.";
                    return false;
                }

                if (zombieConfig.moveSpeed <= 0f)
                {
                    errorMessage = $"Level '{levelId}' zombie '{zombieConfig.zombieId}' has invalid moveSpeed={zombieConfig.moveSpeed}.";
                    return false;
                }

                if (zombieConfig.chaseSpeed <= 0f)
                {
                    errorMessage =
                        $"Level '{levelId}' zombie '{zombieConfig.zombieId}' has invalid chaseSpeed={zombieConfig.chaseSpeed}.";
                    return false;
                }

                if (zombieConfig.detectDistance < 0f)
                {
                    errorMessage =
                        $"Level '{levelId}' zombie '{zombieConfig.zombieId}' has invalid detectDistance={zombieConfig.detectDistance}.";
                    return false;
                }

                if (zombieConfig.attackRange < 0f)
                {
                    errorMessage =
                        $"Level '{levelId}' zombie '{zombieConfig.zombieId}' has invalid attackRange={zombieConfig.attackRange}.";
                    return false;
                }

                if (zombieConfig.attackPower < 0)
                {
                    errorMessage =
                        $"Level '{levelId}' zombie '{zombieConfig.zombieId}' has invalid attackPower={zombieConfig.attackPower}.";
                    return false;
                }

                if (zombieConfig.patrolPath == null || zombieConfig.patrolPath.Count == 0)
                {
                    errorMessage =
                        $"Level '{levelId}' zombie '{zombieConfig.zombieId}' has empty patrolPath. At least one point is required.";
                    return false;
                }

                for (int patrolPointIndex = 0; patrolPointIndex < zombieConfig.patrolPath.Count; patrolPointIndex++)
                {
                    if (zombieConfig.patrolPath[patrolPointIndex] == null)
                    {
                        errorMessage =
                            $"Level '{levelId}' zombie '{zombieConfig.zombieId}' has null patrolPath point at index {patrolPointIndex}.";
                        return false;
                    }
                }
            }

            errorMessage = string.Empty;
            return true;
        }
    }
}
