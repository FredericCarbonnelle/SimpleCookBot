using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Cognitive.CustomVision.Prediction;
using Microsoft.Cognitive.CustomVision.Training;

namespace SimpleCookBot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            if (activity.GetActivityType() == ActivityTypes.Message)
            {
                if (activity.Attachments?.Count >= 1)
                {
                    if (activity.Attachments[0].ContentType == "image/png" || activity.Attachments[0].ContentType == "image/jpeg" || activity.Attachments[0].ContentType == "image/jpg")
                    {
                        MakePrediction(activity);
                    }
                }
                else
                {
                    await Conversation.SendAsync(activity, () => new Dialogs.RootDialog());
                }
            }
            else
            {
                HandleSystemMessage(activity);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private Activity HandleSystemMessage(Activity message)
        {
            string messageType = message.GetActivityType();
            if (messageType == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (messageType == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (messageType == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (messageType == ActivityTypes.Typing)
            {
                // Handle knowing that the user is typing
            }
            else if (messageType == ActivityTypes.Ping)
            {
            }

            return null;
        }

        private async Task MakePrediction(Activity message)
        {
            string trainingKey = "";
            string predictionKey = "";

            // Create the Api, passing in the training key
            TrainingApi trainingApi = new TrainingApi() { ApiKey = trainingKey };

            Guid projectId = new Guid("");
            var project = trainingApi.GetProject(projectId);

            // Create a prediction endpoint, passing in obtained prediction key
            PredictionEndpoint endpoint = new PredictionEndpoint() { ApiKey = predictionKey };

            // The the local test image
            string imageUrl = $"{message.Attachments[0].ContentUrl}";
            MemoryStream image = GetStreamFromUrl(imageUrl);

            // Make a prediction against the new project
            var result = endpoint.PredictImage(project.Id, image);

            ConnectorClient connector = new ConnectorClient(new Uri(message.ServiceUrl));

            // Loop over each prediction and write out the results
            foreach (var c in result.Predictions)
            {
                Activity reply = message.CreateReply($"\t{c.Tag}: {c.Probability:P1}");
                await connector.Conversations.ReplyToActivityAsync(reply);
            }
        }

        private static MemoryStream GetStreamFromUrl(string url)
        {
            byte[] imageData = null;

            using (var wc = new System.Net.WebClient())
                imageData = wc.DownloadData(url);

            return new MemoryStream(imageData);
        }
    }
}