using System;
using System.Collections.Generic;
using System.Diagnostics;
using PureCloudPlatform.Client.V2.Api;
using PureCloudPlatform.Client.V2.Client;
using PureCloudPlatform.Client.V2.Extensions;
using PureCloudPlatform.Client.V2.Extensions.Notifications;
using PureCloudPlatform.Client.V2.Model;
using Newtonsoft.Json;

namespace AgentState
{
    class Program
    {
        static void Main(string[] args)
        {
            // OAuth input
            Console.Write("Enter Client ID: ");
            string clientId = Console.ReadLine();
            Console.Write("Enter Client Secret: ");
            string clientSecret = Console.ReadLine();

            // Group name to get members from
            Console.Write("Enter Group Name: ");
            string groupName = Console.ReadLine();

            // Configure SDK Settings
            string accessToken = GetToken(clientId, clientSecret);
            PureCloudPlatform.Client.V2.Client.Configuration.Default.AccessToken = accessToken;

            // Instantiate APIs
            GroupsApi groupsApi = new GroupsApi();

            // Get the group using the group name
            GroupsSearchResponse theGroup = GetGroup(groupName, groupsApi);

            // Get members of the group
            List<User> users = GetGroupMembers(theGroup, groupsApi);

            // Build the notification handler
            NotificationHandler handler = new NotificationHandler();

            // Subscribe the notifiaciton handler to the presence of group members
            SubscribeToUserGroupPresence(users, handler);

            // Display to confirm websocket client is listening
            Console.WriteLine("Websocket connected, awaiting messages...");

            if (Debugger.IsAttached)
            {
                Console.ReadKey();
            }
        }

        /// <summary>
        /// Subscribe the notification handler to the users' presence and routing statuses.
        /// </summary>
        /// <param name="userList"></param>
        /// <param name="handler"></param>
        private static void SubscribeToUserGroupPresence(List<User> userList, NotificationHandler handler)
        {
            foreach (var user in userList)
            {
                handler.AddSubscription($"v2.users.{user.Id}.presence", typeof(UserPresenceNotification));
                handler.AddSubscription($"v2.users.{user.Id}.routingStatus", typeof(UserRoutingStatusNotification));
            }

            // Listens for User Presence and Routing Status changes
            handler.NotificationReceived += (data) =>
            {
                Console.WriteLine(JsonConvert.SerializeObject(data, Formatting.Indented));

                if (data.GetType() == typeof(NotificationData<UserPresenceNotification>))
                {
                    var presence = (NotificationData<UserPresenceNotification>)data;
                    Console.WriteLine($"New Presence: {presence.EventBody.PresenceDefinition.SystemPresence}");
                    Console.WriteLine("****************************************************************************");
                }
                else if (data.GetType() == typeof(NotificationData<UserRoutingStatusNotification>))
                {
                    var status = (NotificationData<UserRoutingStatusNotification>)data;
                    Console.WriteLine($"New Status: {status.EventBody.RoutingStatus.Status}");
                    Console.WriteLine("****************************************************************************");
                }
            };
        }

        /// <summary>
        /// Get members of a group.
        /// </summary>
        /// <param name="group"></param>
        /// <param name="api"></param>
        /// <returns></returns>
        private static List<User> GetGroupMembers(GroupsSearchResponse group, GroupsApi api)
        {
            // The list that will contain group members
            List<User> members = new List<User>();

            foreach (var grp in group.Results)
            {
                // API call to get members
                UserEntityListing result = api.GetGroupMembers(grp.Id, 25, null, "ASC", null);

                // Add the members to the list
                members = result.Entities;
            }

            return members;
        }

        /// <summary>
        /// Search and Get a PureCloud group using its name or id.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="api"></param>
        /// <returns></returns>
        private static GroupsSearchResponse GetGroup(string name, GroupsApi api)
        {
            // Search criteria is group name with exact value
            GroupSearchCriteria criteria = new GroupSearchCriteria(Value: name,
                                                                   _Operator: GroupSearchCriteria.OperatorEnum.And,
                                                                   Fields: (new List<string> { "name" }),
                                                                   Type: GroupSearchCriteria.TypeEnum.Exact);

            // Add criteria to Group Search Criteria List
            List<GroupSearchCriteria> groupSearchCriteriaList = new List<GroupSearchCriteria>();
            groupSearchCriteriaList.Add(criteria);

            // Build list of Group Search Criteria
            GroupSearchRequest request = new GroupSearchRequest();
            request.Query = groupSearchCriteriaList;

            return api.PostGroupsSearch(request);
        }

        /// <summary>
        /// Request client credentials token from PureCloud
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="clientSecret"></param>
        /// <returns></returns>
        private static string GetToken(string clientId, string clientSecret)
        {
            var accessTokenInfo = Configuration.Default.ApiClient.PostToken(clientId, clientSecret);
            string token = accessTokenInfo.AccessToken;

            return token;
        }
    }
}
