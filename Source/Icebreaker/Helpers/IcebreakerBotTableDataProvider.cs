//----------------------------------------------------------------------------------------------
// <copyright file="IcebreakerBotTableDataProvider.cs" company="UNIFY Solutions">
// Copyright (c) UNIFY Solutions. All rights reserved.
// </copyright>
//----------------------------------------------------------------------------------------------

namespace Icebreaker.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights;
    using Microsoft.Azure;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Auth;
    using Microsoft.WindowsAzure.Storage.Table;

    /// <summary>
    /// Data provider for Azure Table Storage
    /// </summary>
    public class IcebreakerBotTableDataProvider
    {
        private readonly TelemetryClient telemetryClient;
        private readonly Lazy<Task> initializeTask;

        private CloudTable teamsTable;
        private CloudTable usersTable;

        /// <summary>
        /// Initializes a new instance of the <see cref="IcebreakerBotTableDataProvider"/> class.
        /// </summary>
        /// <param name="telemetryClient">The telemetry client to use</param>
        public IcebreakerBotTableDataProvider(TelemetryClient telemetryClient)
        {
            this.telemetryClient = telemetryClient;
            this.initializeTask = new Lazy<Task>(this.InitializeAsync);
        }

        /// <summary>
        /// Updates team installation status in store. If the bot is installed, the info is saved, otherwise info for the team is deleted.
        /// </summary>
        /// <param name="team">The team installation info</param>
        /// <param name="installed">Value that indicates if bot is installed</param>
        /// <returns>Tracking task</returns>
        public async Task UpdateTeamInstallStatusAsync(TeamInstallInfo team, bool installed)
        {
            await this.EnsureInitializedAsync();

            if (installed)
            {
                await this.teamsTable.ExecuteAsync(TableOperation.InsertOrReplace(team));
            }
            else
            {
                await this.teamsTable.ExecuteAsync(TableOperation.Delete(team));
            }
        }

        /// <summary>
        /// Get the list of teams to which the app was installed.
        /// </summary>
        /// <returns>List of installed teams</returns>
        public async Task<IList<TeamInstallInfo>> GetInstalledTeamsAsync()
        {
            await this.EnsureInitializedAsync();
            var installedTeams = new List<TeamInstallInfo>();
            try
            {
                installedTeams = this.teamsTable.ExecuteQuery(new TableQuery<TeamInstallInfo>()).ToList();
            }
            catch (Exception ex)
            {
                this.telemetryClient.TrackException(ex.InnerException);
            }

            return installedTeams;
        }

        /// <summary>
        /// Returns the team that the bot has been installed to
        /// </summary>
        /// <param name="teamId">The team id</param>
        /// <returns>Team that the bot is installed to</returns>
        public async Task<TeamInstallInfo> GetInstalledTeamAsync(string teamId)
        {
            await this.EnsureInitializedAsync();

            // Get team install info
            try
            {
                return await this.teamsTable.QuerySingleItemAsync<TeamInstallInfo>(teamId, teamId);
            }
            catch (Exception ex)
            {
                this.telemetryClient.TrackException(ex.InnerException);
                return null;
            }
        }

        /// <summary>
        /// Get the stored information about the given user
        /// </summary>
        /// <param name="userId">User id</param>
        /// <returns>User information</returns>
        public async Task<UserInfo> GetUserInfoAsync(string userId)
        {
            await this.EnsureInitializedAsync();

            try
            {
                return await this.usersTable.QuerySingleItemAsync<UserInfo>(userId, userId);
            }
            catch (Exception ex)
            {
                this.telemetryClient.TrackException(ex.InnerException);
                return null;
            }
        }

        /// <summary>
        /// Set the user info for the given user
        /// </summary>
        /// <param name="tenantId">Tenant id</param>
        /// <param name="userId">User id</param>
        /// <param name="optedIn">User opt-in status</param>
        /// <param name="serviceUrl">User service URL</param>
        /// <returns>Tracking task</returns>
        public async Task SetUserInfoAsync(string tenantId, string userId, bool optedIn, string serviceUrl)
        {
            await this.EnsureInitializedAsync();

            var userInfo = new UserInfo
            {
                TenantId = tenantId,
                UserId = userId,
                OptedIn = optedIn,
                ServiceUrl = serviceUrl
            };
            await this.usersTable.ExecuteAsync(TableOperation.InsertOrReplace(userInfo));
        }

        /// <summary>
        /// Initializes the database connection.
        /// </summary>
        /// <returns>Tracking task</returns>
        private async Task InitializeAsync()
        {
            this.telemetryClient.TrackTrace("Initializing data store");

            var tableAccountName = CloudConfigurationManager.GetSetting("TableAccountName");
            var tableKey = CloudConfigurationManager.GetSetting("TableAccountKey");
            var teamsCollectionName = CloudConfigurationManager.GetSetting("TableCollectionTeams");
            var usersCollectionName = CloudConfigurationManager.GetSetting("TableCollectionUsers");

            var storageAccount = new CloudStorageAccount(
                    new StorageCredentials(tableAccountName, tableKey), true)
                .CreateCloudTableClient();

            this.teamsTable = storageAccount.GetTableReference(teamsCollectionName);
            this.usersTable = storageAccount.GetTableReference(usersCollectionName);

            await this.teamsTable.CreateIfNotExistsAsync();
            await this.usersTable.CreateIfNotExistsAsync();

            this.telemetryClient.TrackTrace("Data store initialized");
        }

        private async Task EnsureInitializedAsync()
        {
            await this.initializeTask.Value;
        }
    }
}