﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reactive.Subjects;
using System.Runtime.Serialization.Formatters;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Amino
{
    public class SubClient
    {

        private Amino.Client client;
        public bool debug { get; set; }
        private string communityId;



        //headers.
        public Dictionary<string, string> headers = new Dictionary<string, string>();

        //Handles the header stuff
        private Task headerBuilder()
        {
            headers.Clear();
            headers.Add("NDCDEVICEID", client.deviceID);
            headers.Add("Accept-Language", "en-US");
            headers.Add("Content-Type", "application/json; charset=utf-8");
            headers.Add("Host", "service.aminoapps.com");
            headers.Add("Accept-Encoding", "gzip");
            headers.Add("Connection", "Keep-Alive");
            headers.Add("User-Agent", "Apple iPhone13,4 iOS v15.6.1 Main/3.12.9");
            headers.Add("AUID", this.client.userID);
            if (client.sessionID != null) { headers.Add("NDCAUTH", $"sid={client.sessionID}"); }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Creates an instance of the Amino.SubClient object and builds headers
        /// </summary>
        /// <param name="_client"></param>
        /// <param name="_communityId"></param>
        public SubClient(Amino.Client _client, string _communityId)
        {
            if (_client.sessionID == null) { throw new Exception("ErrorCode: 0: Client not logged in"); }
            client = _client;
            debug = client.debug;
            communityId = _communityId;
            headerBuilder();
        }

        /// <summary>
        /// Creates an instance of the Amino.SubClient object and builds headers
        /// </summary>
        /// <param name="_client"></param>
        /// <param name="_communityId"></param>
        public SubClient(Amino.Client _client, int _communityId)
        {
            if (_client.sessionID == null) { throw new Exception("ErrorCode: 0: Client not logged in"); }
            client = _client;
            debug = client.debug;
            communityId = _communityId.ToString();
            headerBuilder();
        }


        /// <summary>
        /// Allows you to get inviite codes of the current community (might require staff permissions)
        /// </summary>
        /// <param name="status"></param>
        /// <param name="start"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public List<Amino.Objects.InviteCode> get_invite_codes(string status = "normal", int start = 0, int size = 25)
        {
            try
            {
                List<Amino.Objects.InviteCode> inviteCodeList = new List<Objects.InviteCode>();
                RestClient client = new RestClient(helpers.BaseUrl);
                RestRequest request = new RestRequest($"/g/s-x{communityId}/community/invitation?status={status}&start={start}&size={size}");
                request.AddHeaders(headers);
                var response = client.ExecuteGet(request);
                if ((int)response.StatusCode != 200) { throw new Exception(response.Content); }
                if (debug) { Trace.WriteLine(response.Content); }
                dynamic jsonObj = (JObject)JsonConvert.DeserializeObject(response.Content);
                JArray inviteCodesJson = jsonObj["communityInvitationList"];
                foreach(JObject code in inviteCodesJson)
                {
                    Amino.Objects.InviteCode invCode = new Objects.InviteCode(code);
                    inviteCodeList.Add(invCode);
                }
                Console.WriteLine(response.Content);
                return inviteCodeList;

            } catch(Exception e) { throw new Exception(e.Message); }

        } 

        /// <summary>
        /// Allows you to generate a Community invite code (might require staff permissions)
        /// </summary>
        /// <param name="duration"></param>
        /// <param name="force"></param>
        /// <returns></returns>
        public Task generate_invite_code(int duration = 0, bool force = true)
        {
            try
            {
                RestClient client = new RestClient(helpers.BaseUrl);
                RestRequest request = new RestRequest($"/g/s-x{communityId}/community/invitation");
                request.AddHeaders(headers);
                JObject data = new JObject();
                data.Add("duration", duration);
                data.Add("force", force);
                data.Add("timestamp", helpers.GetTimestamp() * 1000);
                request.AddJsonBody(JsonConvert.SerializeObject(data));
                request.AddHeader("NDC-MSG-SIG", helpers.generate_signiture(JsonConvert.SerializeObject(data)));
                var response = client.ExecuteGet(request);
                if((int)response.StatusCode != 200) { throw new Exception(response.Content); }
                if(debug) { Trace.WriteLine(response.Content); }
                return Task.CompletedTask;
            }catch(Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        /// <summary>
        /// Allows you to delete an invite code using its ID
        /// </summary>
        /// <param name="inviteId"></param>
        /// <returns></returns>
        public Task delete_invite_code(string inviteId)
        {
            try
            {
                RestClient client = new RestClient(helpers.BaseUrl);
                RestRequest request = new RestRequest($"/g/s-x{communityId}/community/invitation/{inviteId}");
                request.AddHeaders(headers);
                var response = client.Delete(request);
                if((int)response.StatusCode != 200) { throw new Exception(response.Content); }
                if(debug) { Trace.WriteLine(response.Content); }
                return Task.CompletedTask;
            }catch(Exception e) { throw new Exception(e.Message); }
        }

        /// <summary>
        /// Allows you to post a Blog post in the current Community
        /// </summary>
        /// <param name="title"></param>
        /// <param name="content"></param>
        /// <param name="imageList"></param>
        /// <param name="fansOnly"></param>
        /// <param name="backgroundColor"></param>
        /// <returns></returns>
        public Task post_blog(string title, string content, List<byte[]> imageList = null, bool fansOnly = false, string backgroundColor = null)
        {
            try
            {
                JArray mediaList = new JArray();
                JObject extensionData = new JObject();

                if(imageList != null)
                {
                    JArray tempMedia = new JArray();
                    foreach(byte[] bytes in imageList)
                    {
                        tempMedia = new JArray();
                        tempMedia.Add(100);
                        tempMedia.Add(this.client.upload_media(bytes, Types.upload_File_Types.Image));
                        tempMedia.Add(null);
                        mediaList.Add(tempMedia);
                    }
                }
                if(fansOnly) { extensionData.Add("fansOnly", fansOnly); }
                if(backgroundColor != null) { JObject color = new JObject(); color.Add("backgroundColor", backgroundColor); extensionData.Add(new JProperty("style", color)); }

                

                RestClient client = new RestClient(helpers.BaseUrl);
                RestRequest request = new RestRequest($"/x{communityId}/s/blog");
                request.AddHeaders(headers);
                JObject data = new JObject();
                data.Add("address", null);
                data.Add("title", title);
                data.Add("content", content);
                data.Add("mediaList", new JArray(mediaList));
                data.Add(new JProperty("extensions", extensionData));
                data.Add("latitude", 0);
                data.Add("longitude", 0);
                data.Add("eventSource", "GlobalComposeMenu");
                data.Add("timestamp", helpers.GetTimestamp() * 1000);
                request.AddJsonBody(JsonConvert.SerializeObject(data));
                request.AddHeader("NDC-MSG-SIG", helpers.generate_signiture(JsonConvert.SerializeObject(data)));
                var response = client.ExecutePost(request);
                if((int)response.StatusCode != 200) { throw new Exception(response.Content); }
                if(debug) { Trace.WriteLine(response.Content); }

                return Task.CompletedTask;
            }catch(Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        /// <summary>
        /// Allows you to post a Wiki post on the current Community
        /// </summary>
        /// <param name="title"></param>
        /// <param name="content"></param>
        /// <param name="imageList"></param>
        /// <param name="fansOnly"></param>
        /// <param name="backgroundColor"></param>
        /// <returns></returns>
        public Task post_wiki(string title, string content, List<byte[]> imageList = null, bool fansOnly = false, string backgroundColor = null)
        {
            try
            {
                JArray mediaList = new JArray();
                JObject extensionData = new JObject();

                if (imageList != null)
                {
                    JArray tempMedia = new JArray();
                    foreach (byte[] bytes in imageList)
                    {
                        tempMedia = new JArray();
                        tempMedia.Add(100);
                        tempMedia.Add(this.client.upload_media(bytes, Types.upload_File_Types.Image));
                        tempMedia.Add(null);
                        mediaList.Add(tempMedia);
                    }
                }
                if (fansOnly) { extensionData.Add("fansOnly", fansOnly); }
                if (backgroundColor != null) { JObject color = new JObject(); color.Add("backgroundColor", backgroundColor); extensionData.Add(new JProperty("style", color)); }


                RestClient client = new RestClient(helpers.BaseUrl);
                RestRequest request = new RestRequest($"/x{communityId}/s/item");
                request.AddHeaders(headers);
                JObject data = new JObject();
                data.Add("label", title);
                data.Add("content", content);
                data.Add("eventSource", "GlobalComposeMenu");
                data.Add("mediaList", new JArray(mediaList));
                data.Add(new JProperty("extensions", extensionData));
                data.Add("timestamp", helpers.GetTimestamp() * 1000);
                request.AddHeader("NDC-MSG-SIG", helpers.generate_signiture(JsonConvert.SerializeObject(data)));
                request.AddJsonBody(JsonConvert.SerializeObject(data));
                var response = client.ExecutePost(request);
                if((int)response.StatusCode != 200) { throw new Exception(response.Content); }
                if(debug) { Trace.WriteLine(response.Content); }
                return Task.CompletedTask;

            } catch(Exception e) { throw new Exception(e.Message); }
        }

        /// <summary>
        /// Allows you to edit an existing Blog on the current Community
        /// </summary>
        /// <param name="blogId"></param>
        /// <param name="title"></param>
        /// <param name="content"></param>
        /// <param name="imageList"></param>
        /// <param name="fansOnly"></param>
        /// <param name="backgroundColor"></param>
        /// <returns></returns>
        public Task edit_blog(string blogId, string title = null, string content = null, List<byte[]> imageList = null, bool fansOnly = false, string backgroundColor = null)
        {
            try
            {
                JArray mediaList = new JArray();
                JObject extensionData = new JObject();

                if (imageList != null)
                {
                    JArray tempMedia = new JArray();
                    foreach (byte[] bytes in imageList)
                    {
                        tempMedia = new JArray();
                        tempMedia.Add(100);
                        tempMedia.Add(this.client.upload_media(bytes, Types.upload_File_Types.Image));
                        tempMedia.Add(null);
                        mediaList.Add(tempMedia);
                    }
                }
                if (fansOnly) { extensionData.Add("fansOnly", fansOnly); }
                if (backgroundColor != null) { JObject color = new JObject(); color.Add("backgroundColor", backgroundColor); extensionData.Add(new JProperty("style", color)); }



                RestClient client = new RestClient(helpers.BaseUrl);
                RestRequest request = new RestRequest($"/x{communityId}/s/blog");
                request.AddHeaders(headers);
                JObject data = new JObject();
                data.Add("address", null);
                data.Add("title", title);
                data.Add("content", content);
                data.Add("mediaList", new JArray(mediaList));
                data.Add(new JProperty("extensions", extensionData));
                data.Add("latitude", 0);
                data.Add("longitude", 0);
                data.Add("eventSource", "PostDetailView");
                data.Add("timestamp", helpers.GetTimestamp() * 1000);
                request.AddJsonBody(JsonConvert.SerializeObject(data));
                request.AddHeader("NDC-MSG-SIG", helpers.generate_signiture(JsonConvert.SerializeObject(data)));
                var response = client.ExecutePost(request);
                if ((int)response.StatusCode != 200) { throw new Exception(response.Content); }
                if (debug) { Trace.WriteLine(response.Content); }

                return Task.CompletedTask;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        /// <summary>
        /// Allows you to delete a Blog post
        /// </summary>
        /// <param name="blogId"></param>
        /// <returns></returns>
        public Task delete_blog(string blogId)
        {
            try
            {
                RestClient client = new RestClient(helpers.BaseUrl);
                RestRequest request = new RestRequest($"/x{communityId}/s/blog/{blogId}");
                request.AddHeaders(headers);
                var response = client.Delete(request);
                if((int)response.StatusCode != 200) { throw new Exception(response.Content); }
                if(debug) { Trace.WriteLine(response.Content); }
                return Task.CompletedTask;
            } catch(Exception e) { throw new Exception(e.Message); }
        }

        /// <summary>
        /// Allows you to delete a Wiki post
        /// </summary>
        /// <param name="wikiId"></param>
        /// <returns></returns>
        public Task delete_wiki(string wikiId)
        {
            try
            {
                RestClient client = new RestClient(helpers.BaseUrl);
                RestRequest request = new RestRequest($"/x{communityId}/s/item/{wikiId}");
                request.AddHeaders(headers);
                var response = client.Delete(request);
                if((int)response.StatusCode != 200) { throw new Exception(response.Content); }
                if(debug) { Trace.WriteLine(response.Content); }
                return Task.CompletedTask;
            } catch(Exception e) { throw new Exception(e.Message); }
        }

        /// <summary>
        /// Allows you to repost a post
        /// </summary>
        /// <param name="postId"></param>
        /// <param name="type"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public Task repost_blog(string postId, Types.Repost_Types type, string content = null)
        {
            try
            {
                RestClient client = new RestClient(helpers.BaseUrl);
                RestRequest request = new RestRequest($"/x{communityId}/s/blog");
                request.AddHeaders(headers);
                JObject data = new JObject();
                data.Add("content", content);
                data.Add("refObjectId", postId);
                data.Add("refObjectType", (int)type);
                data.Add("type", 2);
                data.Add("timestamp", helpers.GetTimestamp() * 1000);
                request.AddHeader("NDC-MSG-SIG", helpers.generate_signiture(JsonConvert.SerializeObject(data)));
                request.AddJsonBody(JsonConvert.SerializeObject(data));
                var response = client.ExecutePost(request);
                if((int)response.StatusCode != 200) { throw new Exception(response.Content); }
                if(debug) { Trace.WriteLine(response.Content); }
                return Task.CompletedTask;

            }catch(Exception e) { throw new Exception(e.Message); }
        }

        /// <summary>
        /// Allows you to check in on the current Community
        /// </summary>
        /// <param name="timezone"></param>
        /// <returns></returns>
        public Task check_in(int? timezone = null)
        {
            try
            {
                int? tz;
                if (timezone != null) { tz = timezone; } else { tz = helpers.getTimezone(); }
                RestClient client = new RestClient(helpers.BaseUrl);
                RestRequest request = new RestRequest($"/x{communityId}/s/check-in");
                request.AddHeaders(headers);
                JObject data = new JObject();
                data.Add("timestamp", helpers.GetTimestamp() * 1000);
                data.Add("timezone", tz);
                request.AddHeader("NDC-MSG-SIG", helpers.generate_signiture(JsonConvert.SerializeObject(data)));
                request.AddJsonBody(JsonConvert.SerializeObject(data));
                var response = client.ExecutePost(request);
                if((int)response.StatusCode != 200) { throw new Exception(response.Content); }
                if(debug) { Trace.WriteLine(response.Content); }
                return Task.CompletedTask;
            }catch(Exception e) { throw new Exception(e.Message); }
        }

        /// <summary>
        /// Allows you to repair your check in streak using either Amino Coins or Amino+
        /// </summary>
        /// <param name="repairType"></param>
        /// <returns></returns>
        public Task repair_check_in(Types.Repair_Types repairType)
        {
            try
            {
                RestClient client = new RestClient(helpers.BaseUrl);
                RestRequest request = new RestRequest($"/x{communityId}/s/check-in/repair");
                request.AddHeaders(headers);
                JObject data = new JObject();
                data.Add("timestamp", helpers.GetTimestamp() * 1000);
                data.Add("repairMethod", Convert.ToString((int)repairType));
                request.AddHeader("NDC-MSG-SIG", helpers.generate_signiture(JsonConvert.SerializeObject(data)));
                request.AddJsonBody(JsonConvert.SerializeObject(data));
                var response = client.ExecutePost(request);
                if((int)response.StatusCode != 200) { throw new Exception(response.Content); }
                if(debug) { Trace.WriteLine(response.Content); }
                return Task.CompletedTask;
            }catch(Exception e) { throw new Exception(e.Message); }
        }

        /// <summary>
        /// Allows you to claim the check in lottery
        /// </summary>
        /// <param name="timezone"></param>
        /// <returns></returns>
        public Task lottery(int? timezone = null)
        {
            try
            {
                int? tz;
                if (timezone != null) { tz = timezone; } else { tz = helpers.getTimezone(); }
                RestClient client = new RestClient(helpers.BaseUrl);
                RestRequest request = new RestRequest($"/x{communityId}/s/check-in");
                request.AddHeaders(headers);
                JObject data = new JObject();
                data.Add("timestamp", helpers.GetTimestamp() * 1000);
                data.Add("timezone", tz);
                request.AddHeader("NDC-MSG-SIG", helpers.generate_signiture(JsonConvert.SerializeObject(data)));
                request.AddJsonBody(JsonConvert.SerializeObject(data));
                var response = client.ExecutePost(request);
                if ((int)response.StatusCode != 200) { throw new Exception(response.Content); }
                if (debug) { Trace.WriteLine(response.Content); }
                return Task.CompletedTask;
            }
            catch (Exception e) { throw new Exception(e.Message); }
        }

        /// <summary>
        /// Allows you edit your community profile
        /// </summary>
        /// <param name="nickname"></param>
        /// <param name="content"></param>
        /// <param name="icon"></param>
        /// <param name="imageList"></param>
        /// <param name="captionList"></param>
        /// <param name="backgroundColor"></param>
        /// <param name="backgroundImage"></param>
        /// <param name="defaultChatBubbleId"></param>
        /// <returns></returns>
        public Task edit_profile(string nickname = null, string content = null, byte[] icon = null, List<byte[]> imageList = null, List<string> captionList = null, string backgroundColor = null, byte[] backgroundImage = null, string defaultChatBubbleId = null)
        {
            try
            {
                JObject data = new JObject();

                JArray mediaList = new JArray();
                JObject extensionData = new JObject();

                if (imageList != null)
                {
                    JArray tempMedia = new JArray();
                    for(int i = 0; i != imageList.Count; i++)
                    {
                        tempMedia = new JArray();
                        tempMedia.Add(100);
                        tempMedia.Add(this.client.upload_media(imageList[i], Types.upload_File_Types.Image));
                        tempMedia.Add(captionList[i]);
                        mediaList.Add(tempMedia);
                    }
                }
                if (backgroundColor != null) { JObject color = new JObject(); color.Add("backgroundColor", backgroundColor); extensionData.Add(new JProperty("style", color)); }
                if(backgroundImage != null) { JObject bgImg = new JObject(); JArray bgArr = new JArray(); bgArr.Add(100);bgArr.Add(this.client.upload_media(backgroundImage, Types.upload_File_Types.Image));bgArr.Add(null); bgArr.Add(null); bgArr.Add(null); bgImg.Add("backgroundMediaList", new JArray(bgArr)); extensionData.Add(new JProperty("style", bgImg)); }
                if(defaultChatBubbleId != null) { JObject dchtbl = new JObject(); dchtbl.Add("defaultBubbleId", defaultChatBubbleId); extensionData.Add(dchtbl); }


                data.Add("timestamp", helpers.GetTimestamp() * 1000);
                data.Add(new JProperty("extensions", extensionData));
                if(nickname != null) { data.Add("nickname", nickname); }
                if(content != null) { data.Add("content", content); }
                if(icon != null) { data.Add("icon", this.client.upload_media(icon, Types.upload_File_Types.Image)); }

                RestClient client = new RestClient(helpers.BaseUrl);
                RestRequest request = new RestRequest($"/x{communityId}/s/user-profile/{this.client.userID}");
                request.AddHeaders(headers);
                request.AddHeader("NDC-MSG-SIG", helpers.generate_signiture(JsonConvert.SerializeObject(data)));
                request.AddJsonBody(JsonConvert.SerializeObject(data));
                var response = client.ExecutePost(request);
                if ((int)response.StatusCode != 200) throw new Exception(response.Content);
                if(debug) { Trace.WriteLine(response.Content); }
                return Task.CompletedTask;
            }
            catch(Exception e) { throw new Exception(e.Message); }
        }

        /// <summary>
        /// Allows you to vote on a poll
        /// </summary>
        /// <param name="postId"></param>
        /// <param name="optionId"></param>
        /// <returns></returns>
        public Task vote_poll(string postId, string optionId)
        {
            try
            {
                RestClient client = new RestClient(helpers.BaseUrl);
                RestRequest request = new RestRequest($"/x{communityId}/s/blog/{postId}/poll/option/{optionId}/vote");
                JObject data = new JObject();
                data.Add("timestamp", helpers.GetTimestamp() * 1000);
                data.Add("eventSource", "PostDetailView");
                data.Add("value", 1);
                request.AddHeaders(headers);
                request.AddHeader("NDC-MSG-SIG", helpers.generate_signiture(JsonConvert.SerializeObject(data)));
                request.AddJsonBody(JsonConvert.SerializeObject(data));
                var response = client.ExecutePost(request);
                if((int)response.StatusCode != 200) { throw new Exception(response.Content); }
                if(debug) { Trace.WriteLine(response.Content); }
                return Task.CompletedTask;
            }catch(Exception e) { throw new Exception(e.Message); }
        }

        /// <summary>
        /// Allows you to comment on a Blog, Wiki, Reply, Wall
        /// </summary>
        /// <param name="message"></param>
        /// <param name="type"></param>
        /// <param name="targetId"></param>
        /// <param name="isGuest"></param>
        /// <returns></returns>
        public Task comment(string message, Amino.Types.Comment_Types type, string targetId, bool isGuest = false)
        {
            try
            {
                string endPoint = null;
                JObject data = new JObject();
                data.Add("content", message);
                data.Add("strickerId", null);
                data.Add("type", 0);
                data.Add("timestamp", helpers.GetTimestamp() * 1000);
                string comType;
                if (isGuest) { comType = "g-comment"; } else { comType = "comment"; }
                switch(type)
                {
                    case Types.Comment_Types.Blog:
                        data.Add("eventSource", "PostDetailView");
                        endPoint = $"/x{communityId}/s/blog/{targetId}/{comType}";
                        break;
                    case Types.Comment_Types.Wiki:
                        data.Add("eventSource", "PostDetailView");
                        break;
                    case Types.Comment_Types.Reply:
                        data.Add("respondTo", targetId);
                        endPoint = $"/x{communityId}/s/item/{targetId}/{comType}";
                        break;
                    case Types.Comment_Types.User:
                        data.Add("eventSource", "UserProfileView");
                        endPoint = $"/x{communityId}/s/user-profile/{targetId}/{comType}";
                        break;
                }

                RestClient client = new RestClient(helpers.BaseUrl);
                RestRequest request = new RestRequest(endPoint);
                request.AddHeaders(headers);
                request.AddJsonBody(JsonConvert.SerializeObject(data));
                request.AddHeader("NDC-MSG-SIG", helpers.generate_signiture(JsonConvert.SerializeObject(data)));
                var response = client.ExecutePost(request);
                if((int)response.StatusCode != 200) { throw new Exception(response.Content); }
                if(debug) { Trace.WriteLine(response.Content); }
                return Task.CompletedTask;
            }
            catch(Exception e) { throw new Exception(e.Message); }
        }

        /// <summary>
        /// Allows you to delete a comment
        /// </summary>
        /// <param name="commentId"></param>
        /// <param name="type"></param>
        /// <param name="targetId"></param>
        /// <returns></returns>
        public Task delete_comment(string commentId, Amino.Types.Comment_Types type, string targetId)
        {
            try
            {
                string endPoint = null;
                switch(type)
                {
                    case Types.Comment_Types.Blog:
                        endPoint = $"/x{communityId}/s/blog/{targetId}/comment/{commentId}";
                        break;
                    case Types.Comment_Types.Wiki:
                        endPoint = $"/x{communityId}/s/item/{targetId}/comment/{commentId}";
                        break;
                    case Types.Comment_Types.Reply:
                        break;
                    case Types.Comment_Types.User:
                        endPoint = $"/x{communityId}/s/user-profile/{targetId}/comment/{commentId}";
                        break;
                }


                RestClient client = new RestClient(helpers.BaseUrl);
                RestRequest request = new RestRequest(endPoint);
                request.AddHeaders(headers);
                var response = client.Delete(request);
                if((int)response.StatusCode != 200) { throw new Exception(response.Content); }
                if(debug) { Trace.WriteLine(response.Content); }
                return Task.CompletedTask;
            }catch(Exception e) { throw new Exception(e.Message); }
        }
        
        /// <summary>
        /// Allows you to like a Post
        /// </summary>
        /// <param name="postId"></param>
        /// <param name="isWiki"></param>
        /// <returns></returns>
        public Task like_post(string postId, bool isWiki = false)
        {
            try
            {
                string endPoint = null;

                JObject data = new JObject();
                data.Add("value", 4);
                data.Add("timestamp", helpers.GetTimestamp() * 1000);
                if(!isWiki) { data.Add("eventSource", "UserProfileView"); endPoint = $"/x{communityId}/s/blog/{postId}/vote?cv=1.2"; } else { data.Add("eventSource", "PostDetailView"); endPoint = $"/x{communityId}/s/item/{postId}/vote?cv=1.2"; }

                RestClient client = new RestClient(helpers.BaseUrl);
                RestRequest request = new RestRequest(endPoint);
                request.AddHeaders(headers);
                request.AddHeader("NDC-MSG-SIG", helpers.generate_signiture(JsonConvert.SerializeObject(data)));
                request.AddJsonBody(JsonConvert.SerializeObject(data));
                var response = client.ExecutePost(request);
                if((int)response.StatusCode != 200) { throw new Exception(response.Content); }
                if(debug) { Trace.WriteLine(response.Content); }
                return Task.CompletedTask;
            }
            catch(Exception e) { throw new Exception(e.Message); }
        }

        /// <summary>
        /// Allows you to unlike a Post
        /// </summary>
        /// <param name="postId"></param>
        /// <param name="isWiki"></param>
        /// <returns></returns>
        public Task unlike_post(string postId, bool isWiki = false)
        {
            try
            {
                string endPoint = null;
                if(!isWiki) { endPoint = $"/x{communityId}/s/blog/{postId}/vote?eventSource=UserProfileView"; } else { endPoint = $"/x{communityId}/s/item/{postId}/vote?eventSource=PostDetailView"; }
                RestClient client = new RestClient(helpers.BaseUrl);
                RestRequest request = new RestRequest(endPoint);
                request.AddHeaders(headers);
                var response = client.Delete(request);
                if((int)response.StatusCode != 200) { throw new Exception(response.Content); }
                if(debug) { Trace.WriteLine(response.Content); }
                return Task.CompletedTask;
            }catch(Exception e) { throw new Exception(e.Message); } 
        }

        /// <summary>
        /// Allows you to like a comment
        /// </summary>
        /// <param name="commentId"></param>
        /// <param name="targetId"></param>
        /// <param name="targetType"></param>
        /// <returns></returns>
        public Task like_comment(string commentId, string targetId,Types.Comment_Types targetType)
        {
            try
            {
                string _targetType = null;
                string voteType = null;
                string targetValue = null;
                if(targetType == Types.Comment_Types.User) { _targetType = "UserProfileView"; targetValue = "user-profile"; } else { _targetType = "PostDetailView"; targetValue = "blog"; }
                if(targetType == Types.Comment_Types.Wiki) { voteType = "g-vote"; targetValue = "item"; } else { voteType = "vote";  }
                JObject data = new JObject();
                data.Add("timestamp", helpers.GetTimestamp() * 1000);
                data.Add("value", 1);
                data.Add("EventSource", _targetType);

                RestClient client = new RestClient(helpers.BaseUrl);
                RestRequest request = new RestRequest($"/x{communityId}/s/{targetValue}/{targetId}/comment/{commentId}/{voteType}?cv=1.2&value=1");
                request.AddHeaders(headers);
                request.AddHeader("NDC-MSG-SIG", helpers.generate_signiture(JsonConvert.SerializeObject(data)));
                request.AddJsonBody(JsonConvert.SerializeObject(data));
                var response = client.ExecutePost(request);
                if((int)response.StatusCode != 200) { throw new Exception(response.Content); }
                if(debug) { Trace.WriteLine(response.Content); }
                return Task.CompletedTask;

            }catch(Exception e) { throw new Exception(e.Message); }
        }


        /// <summary>
        /// Allows you to unlike a comment
        /// </summary>
        /// <param name="commentId"></param>
        /// <param name="targetId"></param>
        /// <param name="targetType"></param>
        /// <returns></returns>
        public Task unlike_comment(string commentId, string targetId, Amino.Types.Comment_Types targetType)
        {
            try
            {
                string _targetType = null;
                string _eventSource = "PostDetailView";
                if (targetType == Types.Comment_Types.User) { _targetType = "user-profile"; _eventSource = "UserProfileView"; } else if (targetType == Types.Comment_Types.Wiki) { _targetType = "item"; } else { _targetType = "blog"; }
                


                RestClient client = new RestClient(helpers.BaseUrl);
                RestRequest request = new RestRequest($"/x{communityId}/s/{_targetType}/{targetId}/comment/{commentId}/g-vote?eventSource={_eventSource}");
                request.AddHeaders(headers);
                var response = client.Delete(request);
                if((int)response.StatusCode != 200) { throw new Exception(response.Content); }
                if(debug) { Trace.WriteLine(response.Content); }
                return Task.CompletedTask;
            }catch(Exception e) { throw new Exception(e.Message); }
        } 

        /// <summary>
        /// Allows you to upvote a comment
        /// </summary>
        /// <param name="commentId"></param>
        /// <param name="postId"></param>
        /// <returns></returns>
        public Task upvote_comment(string commentId, string postId)
        {
            try
            {
                JObject data = new JObject();
                data.Add("value", 1);
                data.Add("eventSource", "PostDetailView");
                data.Add("timestamp", helpers.GetTimestamp() * 1000);

                RestClient client = new RestClient(helpers.BaseUrl);
                RestRequest request = new RestRequest($"/x{communityId}/s/blog/{postId}/comment/{commentId}/vote?cv=1.2&value=1");
                request.AddHeader("NDC-MSG-SIG", helpers.generate_signiture(JsonConvert.SerializeObject(data)));
                request.AddHeaders(headers);
                request.AddJsonBody(JsonConvert.SerializeObject(data));
                var response = client.ExecutePost(request);
                if((int)response.StatusCode != 200) { throw new Exception(response.Content); }
                if (debug) { Trace.WriteLine(response.Content); }
                return Task.CompletedTask;
            }catch(Exception e) { throw new Exception(e.Message); }
        }

        /// <summary>
        /// Allows you to downvote a comment
        /// </summary>
        /// <param name="commentId"></param>
        /// <param name="postId"></param>
        /// <returns></returns>
        public Task downvote_comment(string commentId, string postId)
        {
            try
            {
                JObject data = new JObject();
                data.Add("value", -1);
                data.Add("eventSource", "PostDetailView");
                data.Add("timestamp", helpers.GetTimestamp() * 1000);

                RestClient client = new RestClient(helpers.BaseUrl);
                RestRequest request = new RestRequest($"/x{communityId}/s/blog/{postId}/comment/{commentId}/vote?cv=1.2&value=1");
                request.AddHeader("NDC-MSG-SIG", helpers.generate_signiture(JsonConvert.SerializeObject(data)));
                request.AddHeaders(headers);
                request.AddJsonBody(JsonConvert.SerializeObject(data));
                var response = client.Delete(request);
                if ((int)response.StatusCode != 200) { throw new Exception(response.Content); }
                if (debug) { Trace.WriteLine(response.Content); }
                return Task.CompletedTask;
            }
            catch (Exception e) { throw new Exception(e.Message); }
        }

        /// <summary>
        /// Allows you to remove your vote from a comment
        /// </summary>
        /// <param name="commentId"></param>
        /// <param name="postId"></param>
        /// <returns></returns>
        public Task unvote_comment(string commentId, string postId)
        {
            try
            {
                RestClient client = new RestClient(helpers.BaseUrl);
                RestRequest request = new RestRequest($"/x{communityId}/s/blog/{postId}/comment/{commentId}/vote?eventSource=PostDetailView");
                var response = client.Delete(request);
                if((int)response.StatusCode != 200) { throw new Exception(response.Content); }
                if (debug) { Trace.WriteLine(response.Content); }
                return Task.CompletedTask;
            }catch(Exception e) { throw new Exception(e.Message); }
        }

        /// <summary>
        /// Allows you to reply to a wall comment
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="commentId"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public Task reply_wall(string userId, string commentId, string message)
        {
            try
            {
                JObject data = new JObject();
                data.Add("content", message);
                data.Add("stackId", null);
                data.Add("respondTo", commentId);
                data.Add("type", 0);
                data.Add("eventSource", "UserProfileView");
                data.Add("timestamp", helpers.GetTimestamp() * 1000);

                RestClient client = new RestClient(helpers.BaseUrl);
                RestRequest request = new RestRequest($"/x{communityId}/s/user-profile/{userId}/comment");
                request.AddHeaders(headers);
                request.AddHeader("NDC-MSG-SIG", helpers.generate_signiture(JsonConvert.SerializeObject(data)));
                request.AddJsonBody(JsonConvert.SerializeObject(data));
                var response = client.ExecutePost(request);
                if ((int)response.StatusCode != 200) { throw new Exception(response.Content); }
                if (debug) { Trace.WriteLine(response.Content); }
                return Task.CompletedTask;
            }catch(Exception e) { throw new Exception(e.Message); }

        }

        /// <summary>
        /// Allows you to set if youre online or offline on the community 
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        public Task set_activity_status(Types.Activity_Status_Types status)
        {
            try
            {
                JObject data = new JObject();
                if (status == Types.Activity_Status_Types.On) { data.Add("onlineStatus", "on"); } else { data.Add("onlineStatus", "off"); }
                data.Add("duration", 86400);
                data.Add("timestamp", helpers.GetTimestamp() * 1000);

                RestClient client = new RestClient(helpers.BaseUrl);
                RestRequest request = new RestRequest($"/x{communityId}/s/user-profile/{this.client.userID}/online-status");
                request.AddHeaders(headers);
                request.AddHeader("NDC-MSG-SIG", helpers.generate_signiture(JsonConvert.SerializeObject(data)));
                request.AddJsonBody(JsonConvert.SerializeObject(data));
                var response = client.ExecutePost(request);
                if((int)response.StatusCode != 200) { throw new Exception(response.Content); }
                if (debug) { Trace.WriteLine(response.Content); }
                return Task.CompletedTask;
            }
            catch(Exception e) { throw new Exception(e.Message); }
        }

        /// <summary>
        /// Allows you to check your notifications on the current Community
        /// </summary>
        /// <returns></returns>
        public Task check_notification()
        {
            try
            {
                RestClient client = new RestClient(helpers.BaseUrl);
                RestRequest request = new RestRequest($"/x{communityId}/s/notification/checked");
                request.AddHeaders(headers);
                var response = client.ExecutePost(request);
                if((int)response.StatusCode != 200) { throw new Exception(response.Content); }
                if(debug) { Trace.WriteLine(response.Content); }
                return Task.CompletedTask;
            }catch(Exception e) { throw new Exception(e.Message); }
        }

        /// <summary>
        /// Allows you to delete a specific notification on the current Community
        /// </summary>
        /// <param name="notificationId"></param>
        /// <returns></returns>
        public Task delete_notification(string notificationId)
        {
            try
            {
                RestClient client = new RestClient(helpers.BaseUrl);
                RestRequest request = new RestRequest($"/x{communityId}/s/notification/{notificationId}");
                request.AddHeaders(headers);
                var response = client.Delete(request);
                if((int)response.StatusCode != 200) { throw new Exception(response.Content); }
                if(debug) { Trace.WriteLine(response.Content); }
                return Task.CompletedTask;
            }catch(Exception e) { throw new Exception(e.Message); }
        }

        /// <summary>
        /// Allows you to clear all notifications on the current Community
        /// </summary>
        /// <returns></returns>
        public Task clear_notifications()
        {
            try
            {
                RestClient client = new RestClient(helpers.BaseUrl);
                RestRequest request = new RestRequest($"/x{communityId}/s/notification");
                request.AddHeaders(headers);
                var response = client.Delete(request);
                if((int)response.StatusCode != 200) { throw new Exception(response.Content); }
                if(debug) { Trace.WriteLine(response.Content); }
                return Task.CompletedTask;
            }catch(Exception e) { throw new Exception(e.Message); }
        }

        /// <summary>
        /// Allows you to send activity time (farm coins) on the current community (NO ARGUMENTS REQUIRED)
        /// </summary>
        /// <returns></returns>
        public Task send_activity_time()
        {
            try
            {
                JObject data = new JObject();
                JArray timeData = new JArray();

                foreach (Dictionary<string, long> timer in helpers.getTimeData())
                {
                    JObject subData = new JObject();

                    subData.Add("start", timer["start"]);
                    subData.Add("end", timer["end"]);

                    timeData.Add(subData);
                }
                int optInAdsFlags = 2147483647;
                int tzf = helpers.TzFilter();
                data.Add("userActiveTimeChunkList", timeData);
                data.Add("OptInAdsFlags", optInAdsFlags);
                data.Add("timestamp", (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds);
                data.Add("timezone", tzf);

                RestClient client = new RestClient(helpers.BaseUrl);
                RestRequest request = new RestRequest($"/x{communityId}/s/community/stats/user-active-time");
                request.AddHeaders(headers);
                request.AddHeader("NDC-MSG-SIG", helpers.generate_signiture(JsonConvert.SerializeObject(data)));
                request.AddJsonBody(JsonConvert.SerializeObject(data));
                var response = client.ExecutePost(request);
                if ((int)response.StatusCode != 200) { throw new Exception(response.Content); }
                if (debug) { Trace.WriteLine(response.Content); }
                return Task.CompletedTask;

            }
            catch(Exception e) { throw new Exception(e.Message); }
        }


        /// <summary>
        /// Allows you to start a chat with multiple people on the current Community
        /// </summary>
        /// <param name="userIds"></param>
        /// <param name="message"></param>
        /// <param name="title"></param>
        /// <param name="content"></param>
        /// <param name="isGlobal"></param>
        /// <param name="publishToGlobal"></param>
        /// <returns></returns>
        public Task start_chat(List<string> userIds, string message, string title = null, string content = null, bool isGlobal = false, bool publishToGlobal = false)
        {
            try
            {
                JObject data = new JObject();
                data.Add("title", title);
                data.Add("inviteeUids", new JArray(userIds));
                data.Add("initialMessageContent", message);
                data.Add("content", content);
                data.Add("timestamp", helpers.GetTimestamp() * 1000);
                if(isGlobal) { data.Add("type", 2); data.Add("eventSource", "GlobalComposeMenu"); } else { data.Add("type", 0); }
                if(publishToGlobal) { data.Add("publishToGlobal", 1); } else { data.Add("publishToGlobal", 0); }

                RestClient client = new RestClient(helpers.BaseUrl);
                RestRequest request = new RestRequest($"/x{communityId}/s/chat/thread");
                request.AddHeaders(headers);
                request.AddHeader("NDC-MSG-SIG", helpers.generate_signiture(JsonConvert.SerializeObject(data)));
                request.AddJsonBody(JsonConvert.SerializeObject(data));
                var response = client.ExecutePost(request);
                if ((int)response.StatusCode != 200) { throw new Exception(response.Content); }
                if (debug) { Trace.WriteLine(response.Content); }
                return Task.CompletedTask;
            }
            catch(Exception e) { throw new Exception(e.Message); }
        }

        /// <summary>
        /// Allows you to start a chat with a single person on the current Community
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="message"></param>
        /// <param name="title"></param>
        /// <param name="content"></param>
        /// <param name="isGlobal"></param>
        /// <param name="publishToGlobal"></param>
        /// <returns></returns>
        public Task start_chat(string userId, string message, string title = null, string content = null, bool isGlobal = false, bool publishToGlobal = false)
        {
            start_chat(new List<string>() { userId }, message, title, content, isGlobal, publishToGlobal);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Allows you to invite multiple people to a chat in the current Community
        /// </summary>
        /// <param name="userIds"></param>
        /// <param name="chatId"></param>
        /// <returns></returns>
        public Task invite_to_chat(List<string> userIds, string chatId)
        {
            try
            {
                JObject data = new JObject();
                data.Add("uids", new JArray(userIds));
                data.Add("timestamp", helpers.GetTimestamp() * 1000);

                RestClient client = new RestClient(helpers.BaseUrl);
                RestRequest request = new RestRequest($"/x{communityId}/s/chat/thread/{chatId}/member/invite");
                request.AddHeaders(headers);
                request.AddHeader("NDC-MSG-SIG", helpers.generate_signiture(JsonConvert.SerializeObject(data)));
                request.AddJsonBody(JsonConvert.SerializeObject(data));
                var response = client.ExecutePost(request);
                if ((int)response.StatusCode != 200) { throw new Exception(response.Content); }
                if (debug) { Trace.WriteLine(response.Content); }
                return Task.CompletedTask;

            }
            catch(Exception e) { throw new Exception(e.Message); }
        }

        /// <summary>
        /// Allows you to invite a single person to a chat in the current Community
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="chatId"></param>
        /// <returns></returns>
        public Task invite_to_chat(string userId, string chatId)
        {
            invite_to_chat(new List<string>() { userId }, chatId);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Allows you to add a user to your favorites
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public Task add_to_favorites(string userId)
        {
            try
            {
                RestClient client = new RestClient(helpers.BaseUrl);
                RestRequest request = new RestRequest($"/x{communityId}/s/user-group/quick-access/{userId}");
                request.AddHeaders(headers);
                var response = client.ExecutePost(request);
                if ((int)response.StatusCode != 200) { throw new Exception(response.Content); }
                if (debug) { Trace.WriteLine(response.Content); }
                return Task.CompletedTask;

            }
            catch(Exception e) { throw new Exception(e.Message); }
        }

        /// <summary>
        /// Allows you to send coins to a user post
        /// </summary>
        /// <param name="targetId">The ID of the target</param>
        /// <param name="coins">The amount of Coins to send (max. 500)</param>
        /// <param name="type">The type of Object you are sending coins to</param>
        /// <param name="transactionId">The ID of the current transaction, can be left empy</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public Task send_coins(string targetId, int coins,Types.Send_Coin_Targets type, string transactionId = null)
        {
            if(transactionId == null) { transactionId = helpers.generate_transaction_id(); }
            string endpoint = "";
            try
            {
                JObject data = new JObject();
                JObject sub = new JObject();
                switch(type)
                {
                    case Types.Send_Coin_Targets.Chat:
                        endpoint = $"/x{communityId}/s/chat/thread/{targetId}/tipping";
                        break;
                    case Types.Send_Coin_Targets.Blog:
                        endpoint = $"/x{communityId}/s/blog/{targetId}/tipping";
                        break;
                    case Types.Send_Coin_Targets.Wiki:
                        endpoint = $"/x{communityId}/s/tipping";
                        data.Add("objectId", targetId);
                        data.Add("objectType", 2);
                        break;
                }
                data.Add("coins", coins);
                data.Add("timestamp", helpers.GetTimestamp() * 1000);
                sub.Add("transactionId", transactionId);
                data.Add("tippingContext", sub);

                RestClient client = new RestClient(helpers.BaseUrl);
                RestRequest request = new RestRequest(endpoint);
                request.AddHeaders(headers);
                request.AddHeader("NDC-MSG-SIG", helpers.generate_signiture(JsonConvert.SerializeObject(data)));
                request.AddJsonBody(JsonConvert.SerializeObject(data));
                var response = client.ExecutePost(request);
                if ((int)response.StatusCode != 200) { throw new Exception(response.Content); }
                if (debug) { Trace.WriteLine(response.Content); }
                return Task.CompletedTask;
            }
            catch(Exception e) { throw new Exception(e.Message); }
        }

        public Task thank_tip(string chatId, string userId)
        {
            try
            {
                RestClient client = new RestClient(helpers.BaseUrl);
                RestRequest request = new RestRequest($"/x{communityId}/s/chat/thread/{chatId}/tipping/tipped-users/{userId}/thank");
                var response = client.ExecutePost(request);
                if((int)response.StatusCode != 200) { throw new Exception(response.Content); }
                if(debug) { Trace.WriteLine(response.Content); }
                return Task.CompletedTask;
            }catch(Exception e) { throw new Exception(e.Message); }
        }

        public Task follow(List<string> userIds)
        {
            try
            {
                JObject data = new JObject();
                data.Add("timestamp", helpers.GetTimestamp() * 1000);
                data.Add("targetUidList", new JArray(userIds));
                RestClient client = new RestClient(helpers.BaseUrl);
                RestRequest request = new RestRequest($"/x{communityId}/s/user-profile/{this.client.userID}/joined");
                request.AddHeaders(headers);
                request.AddHeader("NDC-MSG-SIG", helpers.generate_signiture(JsonConvert.SerializeObject(data)));
                request.AddJsonBody(JsonConvert.SerializeObject(data));
                var response = client.ExecutePost(request);
                if ((int)response.StatusCode != 200) { throw new Exception(response.Content); }
                if (debug) { Trace.WriteLine(response.Content); }
                return Task.CompletedTask;
            }
            catch (Exception e) { throw new Exception(e.Message); }
        }

        public Task follow(string userId)
        {
            try
            {
                follow(new List<string>() { userId });
                return Task.CompletedTask;
            }
            catch (Exception e) { throw new Exception(e.Message); }
        }

        public Task unfollow(string userId)
        {
            try
            {
                RestClient client = new RestClient(helpers.BaseUrl);
                RestRequest request = new RestRequest($"/x{communityId}/s/user-profile/{this.client.userID}/joined/{userId}");
                request.AddHeaders(headers);
                var response = client.Delete(request);
                if ((int)response.StatusCode != 200) { throw new Exception(response.Content); }
                if (debug) { Trace.WriteLine(response.Content); }
                return Task.CompletedTask;
            }catch(Exception e) { throw new Exception(e.Message); }
        }

        public Task block(string userId)
        {
            try
            {
                RestClient client = new RestClient(helpers.BaseUrl);
                RestRequest request = new RestRequest($"/x{communityId}/s/block/{userId}");
                request.AddHeaders(headers);
                var response = client.ExecutePost(request);
                if((int)response.StatusCode != 200) { throw new Exception(response.Content); }
                if (debug) { Trace.WriteLine(response.Content); }
                return Task.CompletedTask;
            }
            catch (Exception e) { throw new Exception(e.Message); }
        }

        public Task unblock(string userId)
        {
            try
            {
                RestClient client = new RestClient(helpers.BaseUrl);
                RestRequest request = new RestRequest($"/x{communityId}/s/block/{userId}");
                request.AddHeaders(headers);
                var response = client.Delete(request);
                if ((int)response.StatusCode != 200) { throw new Exception(response.Content); }
                if (debug) { Trace.WriteLine(response.Content); }
                return Task.CompletedTask;

            }
            catch (Exception e) { throw new Exception(e.Message); }
        }

        public Task visit(string userId)
        {
            try
            {
                RestClient client = new RestClient(helpers.BaseUrl);
                RestRequest request = new RestRequest($"/x{communityId}/s/user-profile/{userId}?action=visit");
                request.AddHeaders(headers);
                var response = client.ExecuteGet(request);
                if ((int)response.StatusCode != 200) { throw new Exception(response.Content); }
                if (debug) { Trace.WriteLine(response.Content); }
                return Task.CompletedTask;
            }catch(Exception e) { throw new Exception(e.Message); }
        }

        public Task flag(string targetId, string reason, Types.Flag_Types flagType, Types.Flag_Targets targetType, bool asGuest = false)
        {
            try
            {
                int _objectType = 0;
                string flg = "";
                int _flagType = 0;
                if (asGuest) { flg = "g-flag"; } else { flg = "flag"; }
                switch (flagType)
                {
                    case Types.Flag_Types.Aggression:
                        _flagType = 0;
                        break;
                    case Types.Flag_Types.Spam:
                        _flagType = 2;
                        break;
                    case Types.Flag_Types.Off_Topic:
                        _flagType = 4;
                        break;
                    case Types.Flag_Types.Violence:
                        _flagType = 106;
                        break;
                    case Types.Flag_Types.Intolerance:
                        _flagType = 107;
                        break;
                    case Types.Flag_Types.Suicide:
                        _flagType = 108;
                        break;
                    case Types.Flag_Types.Trolling:
                        _flagType = 109;
                        break;
                    case Types.Flag_Types.Pronography:
                        _flagType = 110;
                        break;
                    default:
                        _flagType = 0;
                        break;
                }
                switch (targetType)
                {
                    case Types.Flag_Targets.User:
                        _objectType = 0;
                        break;
                    case Types.Flag_Targets.Blog:
                        _objectType = 1;
                        break;
                    case Types.Flag_Targets.Wiki:
                        _objectType = 2;
                        break;
                    default:
                        _objectType = 0;
                        break;
                }

                JObject data = new JObject();
                data.Add("timestamp", helpers.GetTimestamp() * 1000);
                data.Add("flagType", _flagType);
                data.Add("message", reason);
                data.Add("objectId", targetId);
                data.Add("objectType", _objectType);

                RestClient client = new RestClient(helpers.BaseUrl);
                RestRequest request = new RestRequest($"/x{communityId}/s/{flg}");
                request.AddHeaders(headers);
                request.AddHeader("NDC-MSG-SIG", helpers.generate_signiture(JsonConvert.SerializeObject(data)));
                request.AddJsonBody(JsonConvert.SerializeObject(data));
                var response = client.ExecutePost(request);
                if ((int)response.StatusCode != 200) { throw new Exception(response.Content); }
                if (debug) { Trace.WriteLine(response.Content); }
                return Task.CompletedTask;

            }
            catch (Exception e) { throw new Exception(e.Message); }
        }

        public Objects.Message send_message(string message, string chatId, Types.Message_Types messageType = Types.Message_Types.General, string replyTo = null, List<string> mentionUserIds = null)
        {
            try
            {
                List<JObject> mentions = new List<JObject>();
                if(mentionUserIds == null) { mentionUserIds = new List<string>(); } else
                {
                    foreach(string user in mentionUserIds)
                    {
                        JObject _mention = new JObject();
                        _mention.Add("uid", user);
                        mentions.Add(_mention);
                    }
                }
                message = message.Replace("<$", "").Replace("$>", "");
                JObject data = new JObject();
                JObject attachementSub = new JObject();
                JObject extensionSub = new JObject();
                JObject extensionSuBArray = new JObject();
                data.Add("type", (int)messageType);
                data.Add("content", message);
                data.Add("clientRefId", helpers.GetTimestamp() / 10 % 1000000000);
                data.Add("timestamp", helpers.GetTimestamp() * 1000);
                attachementSub.Add("objectId", null);
                attachementSub.Add("objectType", null);
                attachementSub.Add("link", null);
                attachementSub.Add("title", null);
                attachementSub.Add("content", null);
                attachementSub.Add("mediaList", null);
                extensionSuBArray.Add("link", null);
                extensionSuBArray.Add("mediaType", 100);
                extensionSuBArray.Add("mediaUploadValue", null);
                extensionSuBArray.Add("mediaUploadValueContentType", "image/jpg");
                extensionSub.Add("mentionedArray", new JArray(mentions));
                extensionSub.Add("linkSnippetList", new JArray(extensionSuBArray));
                data.Add("attachedObject", attachementSub);
                data.Add("extensions", extensionSub);
                if(replyTo != null) { data.Add("replyMessageId", replyTo); }

                RestClient client = new RestClient(helpers.BaseUrl);
                RestRequest request = new RestRequest($"/x{communityId}/s/chat/thread/{chatId}/message");
                request.AddHeaders(headers);
                request.AddHeader("NDC-MSG-SIG", helpers.generate_signiture(JsonConvert.SerializeObject(data)));
                request.AddJsonBody(JsonConvert.SerializeObject(data));
                var response = client.ExecutePost(request);
                if ((int)response.StatusCode != 200) { throw new Exception(response.Content); }
                if (debug) { Trace.WriteLine(response.Content); }
                return new Objects.Message(JObject.Parse(response.Content));
            }
            catch (Exception e) { throw new Exception(e.Message); }
        }

        public Task send_file_message(string chatId, byte[] file, Types.upload_File_Types fileType)
        {
            JObject data = new JObject();
            JObject attachementSub = new JObject();
            JObject extensionSub = new JObject();
            JObject extensionSuBArray = new JObject();
            data.Add("clientRefId", helpers.GetTimestamp() / 10 % 1000000000);
            data.Add("timestamp", helpers.GetTimestamp() * 1000);
            data.Add("content", null);
            data.Add("type", 0);
            attachementSub.Add("objectId", null);
            attachementSub.Add("objectType", null);
            attachementSub.Add("link", null);
            attachementSub.Add("title", null);
            attachementSub.Add("content", null);
            attachementSub.Add("mediaList", null);
            extensionSuBArray.Add("link", null);
            extensionSuBArray.Add("mediaType", 100);
            extensionSuBArray.Add("mediaUploadValue", null);
            extensionSuBArray.Add("mediaUploadValueContentType", "image/jpg");
            extensionSub.Add("linkSnippetList", new JArray(extensionSuBArray));
            data.Add("attachedObject", attachementSub);
            data.Add("extensions", extensionSub);

            switch(fileType)
            {
                case Types.upload_File_Types.Image:
                    data.Add("mediaType", 100);
                    data.Add("mediaUploadValueContentType", "image/jpg");
                    data.Add("mediaUhqEnabled", true);
                    break;
                case Types.upload_File_Types.Gif:
                    data.Add("mediaType", 100);
                    data.Add("mediaUploadValueContentType", "image/gif");
                    data.Add("enableUhqEnabled", true);
                    break;
                case Types.upload_File_Types.Audio:
                    data.Add("type", 2);
                    data.Add("mediaType", 110);
                    break;
            }
            data.Add("mediaUploadValue", Encoding.UTF8.GetString(Convert.FromBase64String(Convert.ToBase64String(file))));
           

            RestClient client = new RestClient(helpers.BaseUrl);
            RestRequest request = new RestRequest($"/x{communityId}/s/chat/thread/{chatId}/message");
            request.AddHeaders(headers);
            request.AddHeader("NDC-MSG-SIG", helpers.generate_signiture(JsonConvert.SerializeObject(data)));
            request.AddJsonBody(JsonConvert.SerializeObject(data));
            var response = client.ExecutePost(request);
            if ((int)response.StatusCode != 200) { throw new Exception(response.Content); }
            if (debug) { Trace.WriteLine(response.Content); }
            return Task.CompletedTask;
        }

        public Task send_file_message(string chatId, string filePath, Types.upload_File_Types fileType)
        {
            try
            {
                send_file_message(chatId, File.ReadAllBytes(filePath), fileType);
                return Task.CompletedTask;
            } catch (Exception e) { throw new Exception(e.Message); }
            
        }

        public Task send_embed(string chatId, string content = null, string embedId = null, string embedLink = null, string embedTitle = null, string embedContent = null, byte[] embedImage = null)
        {

            try
            {

                RestClient client = new RestClient(helpers.BaseUrl);
                RestRequest request = new RestRequest($"/x{communityId}/s/chat/thread/{chatId}/message");
                JObject data = new JObject
                {
                    { "type", 0 },
                    { "content", content },
                    { "clientRefId", helpers.GetTimestamp() / 10 % 1000000000 },
                    { "attachedObject", new JObject()
                        {
                            { "objectId", embedId },
                            { "objectType", null },
                            { "link", embedLink },
                            { "title", embedTitle },
                            { "content", embedContent },
                            { "mediaList", (embedImage == null) ? null : new JArray() { new JArray() { 100, this.client.upload_media(embedImage, Types.upload_File_Types.Image), null } } }
                        }
                    },
                    { "extensions", new JObject()
                        {
                            { "mentionedArray", new JArray() }
                        }
                    },
                    { "timestamp", helpers.GetTimestamp() }
                };


                request.AddHeaders(headers);
                request.AddJsonBody(JsonConvert.SerializeObject(data));
                request.AddHeader("NDC-MSG-SIG", helpers.generate_signiture(JsonConvert.SerializeObject(data)));
                var response = client.ExecutePost(request);
                if ((int)response.StatusCode != 200) { throw new Exception(response.Content); }
                if (debug) { Trace.WriteLine(response.Content); }
                return Task.CompletedTask;


            }
            catch(Exception e) { throw new Exception(e.Message); }
        }

        public Task send_embed(string chatId, string content = null, string embedId = null, string embedLink = null, string embedTitle = null, string embedContent = null, string embedImagePath = null)
        {
            send_embed(chatId, content, embedId, embedLink, embedTitle, embedContent, (embedImagePath == null) ? null : File.ReadAllBytes(embedImagePath));
            return Task.CompletedTask;
        }

        public Task send_sticker(string chatId, string stickerId)
        {
            try
            {


                JObject data = new JObject();
                JObject attachementSub = new JObject();
                JObject extensionSub = new JObject();
                JObject extensionSuBArray = new JObject();
                data.Add("type", 3);
                data.Add("content", null);
                data.Add("clientRefId", helpers.GetTimestamp() / 10 % 1000000000);
                data.Add("timestamp", helpers.GetTimestamp() * 1000);
                attachementSub.Add("objectId", null);
                attachementSub.Add("objectType", null);
                attachementSub.Add("link", null);
                attachementSub.Add("title", null);
                attachementSub.Add("content", null);
                attachementSub.Add("mediaList", null);
                extensionSuBArray.Add("link", null);
                extensionSuBArray.Add("mediaType", 100);
                extensionSuBArray.Add("mediaUploadValue", null);
                extensionSuBArray.Add("mediaUploadValueContentType", "image/jpg");
                extensionSub.Add("mentionedArray", new JArray());
                extensionSub.Add("linkSnippetList", new JArray(extensionSuBArray));
                data.Add("attachedObject", attachementSub);
                data.Add("extensions", extensionSub);
                data.Add("stickerId", stickerId);

                RestClient client = new RestClient(helpers.BaseUrl);
                RestRequest request = new RestRequest($"/x{communityId}/s/chat/thread/{chatId}/message");
                request.AddHeaders(headers);
                request.AddHeader("NDC-MSG-SIG", helpers.generate_signiture(JsonConvert.SerializeObject(data)));
                request.AddJsonBody(JsonConvert.SerializeObject(data));
                var response = client.ExecutePost(request);
                if ((int)response.StatusCode != 200) { throw new Exception(response.Content); }
                if (debug) { Trace.WriteLine(response.Content); }
                return Task.CompletedTask;

            }
            catch(Exception e) { throw new Exception(e.Message); }
        }

        public Task delete_message(string chatId, string messageId, bool asStaff = false, string reason = null)
        {
            try
            {
                string endPoint = $"/x{communityId}/s/chat/thread/{chatId}/message/{messageId}";
                JObject data = new JObject();
                data.Add("adminOpName", 102);
                data.Add("timestamp", helpers.GetTimestamp() * 1000);
                if((asStaff && reason != null)) { data.Add("adminOpNote", reason); }
                if(asStaff) { endPoint = endPoint + "/admin"; }

                RestClient client = new RestClient(helpers.BaseUrl);
                RestRequest request = new RestRequest(endPoint);
                request.AddHeaders(headers);
                if(asStaff)
                {
                    request.AddHeader("NDC-MSG-SIG", helpers.generate_signiture(JsonConvert.SerializeObject(data)));
                    request.AddJsonBody(JsonConvert.SerializeObject(data));
                    var response = client.ExecutePost(request);
                    if ((int)response.StatusCode != 200) { throw new Exception(response.Content); }
                    if (debug) { Trace.WriteLine(response.Content); }
                } else
                {
                    var response = client.Delete(request);
                    if((int)response.StatusCode != 200) { throw new Exception(response.Content); }
                    if(debug) { Trace.WriteLine(response.Content); }
                }
                return Task.CompletedTask;
            }
            catch(Exception e) { throw new Exception(e.Message); }
        }

        public Task mark_as_read(string chatId, string messageId)
        {
            try
            {
                JObject data = new JObject();
                data.Add("messageId", messageId);
                data.Add("timestamp", helpers.GetTimestamp() * 1000);

                RestClient client = new RestClient(helpers.BaseUrl);
                RestRequest request = new RestRequest($"/x{communityId}/s/chat/thread/{chatId}/mark-as-read");
                request.AddHeaders(headers);
                request.AddHeader("NDC-MSG-SIG", helpers.generate_signiture(JsonConvert.SerializeObject(data)));
                request.AddJsonBody(JsonConvert.SerializeObject(data));
                var response = client.ExecutePost(request);
                if ((int)response.StatusCode != 200) { throw new Exception(response.Content); }
                if (debug) { Trace.WriteLine(response.Content); }
                return Task.CompletedTask;
            }
            catch(Exception e) { throw new Exception(e.Message); }
        }


        public Task transfer_host(string chatId, List<string> userIds)
        {
            try
            {
                JObject data = new JObject();
                data.Add("uidList", new JArray(userIds));
                data.Add("timestamp", helpers.GetTimestamp() * 1000);
                RestClient client = new RestClient(helpers.BaseUrl);
                RestRequest request = new RestRequest($"/x{communityId}/s/chat/thread/{chatId}/transfer-organizer");
                request.AddHeaders(headers);
                request.AddHeader("NDC-MSG-SIG", helpers.generate_signiture(JsonConvert.SerializeObject(data)));
                request.AddJsonBody(JsonConvert.SerializeObject(data));
                var response = client.ExecutePost(request);
                if ((int)response.StatusCode != 200) { throw new Exception(response.Content); }
                if (debug) { Trace.WriteLine(response.Content); }
                return Task.CompletedTask;

            }
            catch(Exception e) { throw new Exception(e.Message); }
        }

        public Task transfer_host(string chatId, string userId)
        {
            try
            {
                return transfer_host(chatId, new List<string> { userId });
            }catch(Exception e) { throw new Exception(e.Message); }
        }

        public Task accept_host(string chatId, string requestId)
        {
            try
            {
                JObject data = new JObject();
                RestClient client = new RestClient(helpers.BaseUrl);
                RestRequest request = new RestRequest($"/x{communityId}/s/chat/thread/{chatId}/transfer-organizer/{requestId}/accept");
                request.AddHeaders(headers);
                request.AddHeader("NDC-MSG-SIG", helpers.generate_signiture(JsonConvert.SerializeObject(data)));
                request.AddJsonBody(JsonConvert.SerializeObject(data));
                var response = client.ExecutePost(request);
                if ((int)response.StatusCode != 200) { throw new Exception(response.Content); }
                if (debug) { Trace.WriteLine(response.Content); }
                return Task.CompletedTask;
            }
            catch(Exception e) { throw new Exception(e.Message); }
        }

        public Task join_chat(string chatId)
        {
            try
            {
                RestClient client = new RestClient(helpers.BaseUrl);
                RestRequest request = new RestRequest($"/x{communityId}/s/chat/thread/{chatId}/member/{this.client.userID}");
                request.AddHeaders(headers);
                var response = client.ExecutePost(request);
                if ((int)response.StatusCode != 200) { throw new Exception(response.Content); }
                if (debug) { Trace.WriteLine(response.Content); }
                return Task.CompletedTask;
            }
            catch(Exception e) { throw new Exception(e.Message); }
        }

        public Task leave_chat(string chatId)
        {
            try
            {
                RestClient client = new RestClient(helpers.BaseUrl);
                RestRequest request = new RestRequest($"/x{communityId}/s/chat/thread/{chatId}/member/{this.client.userID}");
                request.AddHeaders(headers);
                var response = client.Delete(request);
                if ((int)response.StatusCode != 200) { throw new Exception(response.Content); }
                if (debug) { Trace.WriteLine(response.Content); }
                return Task.CompletedTask;
            }
            catch (Exception e) { throw new Exception(e.Message); }
        }

        public List<Amino.Objects.UserFollowings> get_user_following(string userId, int start = 0, int size = 25)
        {
            if (start < 0) { throw new Exception("start cannot be lower than 0"); }
            try
            {
                List<Objects.UserFollowings> userFollowingsList = new List<Objects.UserFollowings>();
                RestClient client = new RestClient(helpers.BaseUrl);
                RestRequest request = new RestRequest($"/x{communityId}/s/user-profile/{userId}/joined?start={start}&size={size}");
                request.AddHeaders(headers);
                var response = client.ExecuteGet(request);
                if ((int)response.StatusCode != 200) { throw new Exception(response.Content); }
                if (debug) { Trace.WriteLine(response.Content); }
                dynamic jsonObj = (JObject)JsonConvert.DeserializeObject(response.Content);
                JArray userFollowings = jsonObj["userProfileList"];
                foreach (JObject following in userFollowings)
                {
                    Objects.UserFollowings _following = new Objects.UserFollowings(following);
                    userFollowingsList.Add(_following);
                }
                return userFollowingsList;

            }
            catch (Exception e) { throw new Exception(e.Message); }
        }

        public List<Objects.UserFollowings> get_user_followers(string userId, int start = 0, int size = 25)
        {
            if (start < 0) { throw new Exception("start cannot be lower than 0"); }
            try
            {
                List<Objects.UserFollowings> userFollowerList = new List<Objects.UserFollowings>();
                RestClient client = new RestClient(helpers.BaseUrl);
                RestRequest request = new RestRequest($"/x{communityId}/s/user-profile/{userId}/member?start={start}&size={size}");
                request.AddHeaders(headers);
                var response = client.ExecuteGet(request);
                if ((int)response.StatusCode != 200) { throw new Exception(response.Content); }
                if (debug) { Trace.WriteLine(response.Content); }
                dynamic jsonObj = (JObject)JsonConvert.DeserializeObject(response.Content);
                JArray userFollowers = jsonObj["userProfileList"];
                foreach (JObject follower in userFollowers)
                {
                    Objects.UserFollowings _follower = new Objects.UserFollowings(follower);
                    userFollowerList.Add(_follower);
                }
                return userFollowerList;

            }
            catch (Exception e) { throw new Exception(e.Message); }
        }

        public Objects.UserProfile get_user_info(string userId)
        {
            try
            {
                RestClient client = new RestClient(helpers.BaseUrl);
                RestRequest request = new RestRequest($"/x{communityId}/s/user-profile/{userId}");
                request.AddHeaders(headers);
                var response = client.ExecuteGet(request);
                if ((int)response.StatusCode != 200) { throw new Exception(response.Content); }
                if (debug) { Trace.WriteLine(response.Content); }
                Amino.Objects.UserProfile profile = new Amino.Objects.UserProfile((JObject)JObject.Parse(response.Content)["userProfile"]);
                return profile;
            }
            catch(Exception e) { throw new Exception(e.Message); }
        }

        public Task comment(string message, Types.Comment_Types type, string objectId)
        {
            string _eventSource;
            bool _isReply = false;
            try
            {
                RestClient client = new RestClient(helpers.BaseUrl);
                RestRequest request = new RestRequest();
                request.AddHeaders(headers);
                JObject data = new JObject();
                data.Add("content", message);
                data.Add("stickerId", null);
                data.Add("type", 0);
                data.Add("timestamp", helpers.GetTimestamp() * 1000);


                switch (type)
                {
                    case Types.Comment_Types.User:
                        request.Resource = $"/x{communityId}/s/user-profile/{objectId}/g-comment";
                        _eventSource = "UserProfileView";
                        break;
                    case Types.Comment_Types.Blog:
                        request.Resource = $"/x{communityId}/s/blog/{objectId}/g-comment";
                        _eventSource = "PostDetailView";
                        break;
                    case Types.Comment_Types.Wiki:
                        request.Resource = $"/x{communityId}/s/item/{objectId}/g-comment";
                        _eventSource = "PostDetailView";
                        break;
                    case Types.Comment_Types.Reply:
                        _isReply = true;
                        _eventSource = "";
                        break;
                    default:
                        request.Resource = $"/x{communityId}/s/user-profile/{objectId}/g-comment";
                        _eventSource = "UserProfileView";
                        break;
                }
                if (!_isReply) { data.Add("eventSource", _eventSource); } else { data.Add("respondTo", objectId); }
                request.AddHeader("NDC-MSG-SIG", helpers.generate_signiture(System.Text.Json.JsonSerializer.Serialize(data.ToString())));
                request.AddJsonBody(data);
                var response = client.ExecutePost(request);
                if ((int)response.StatusCode != 200) { throw new Exception(response.Content); }
                if (debug) { Trace.WriteLine(response.Content); }
                return Task.CompletedTask;
            }
            catch (Exception e) { throw new Exception(e.Message); }
        }
        public List<Objects.UserVisitor> get_user_visitors(string userId, int start = 0, int size = 25)
        {
            if (start < 0) { throw new Exception("start cannot be lower than 0"); }
            try
            {
                List<Objects.UserVisitor> userVisitorList = new List<Objects.UserVisitor>();
                RestClient client = new RestClient(helpers.BaseUrl);
                RestRequest request = new RestRequest($"/x{communityId}/s/user-profile/{userId}/visitors?start={start}&size={size}");
                request.AddHeaders(headers);
                var response = client.ExecuteGet(request);
                if ((int)response.StatusCode != 200) { throw new Exception(response.Content); }
                if (debug) { Trace.WriteLine(response.Content); }
                dynamic jsonObj = (JObject)JsonConvert.DeserializeObject(response.Content);
                JArray userVisitors = jsonObj["visitors"];
                foreach (JObject visitor in userVisitors)
                {
                    Objects.UserVisitor _visitor = new Objects.UserVisitor(visitor, jsonObj);
                    userVisitorList.Add(_visitor);
                }
                return userVisitorList;
            }
            catch (Exception e) { throw new Exception(e.Message); }
        }

        public Objects.UserCheckins get_user_checkins(string userId)
        {
            RestClient client = new RestClient(helpers.BaseUrl);
            RestRequest request = new RestRequest($"/x{communityId}/s/check-in/stats/{userId}?timezone={helpers.getTimezone()}");
            request.AddHeaders(headers);
            var response = client.ExecuteGet(request);
            if ((int)response.StatusCode != 200) { throw new Exception(response.Content); }
            if (debug) { Trace.WriteLine(response.Content); }
            return new Objects.UserCheckins(JObject.Parse(response.Content));
        }

        public List<Objects.Blog> get_user_blogs(string userId, int start = 0, int size = 25)
        {
            List<Objects.Blog> blogs = new List<Objects.Blog>();
            RestClient client = new RestClient(helpers.BaseUrl);
            RestRequest request = new RestRequest($"/x{communityId}/s/blog?type=user&q={userId}&start={start}&size={size}");
            request.AddHeaders(headers);
            var response = client.ExecuteGet(request);
            if ((int)response.StatusCode != 200) { throw new Exception(response.Content); }
            if (debug) { Trace.WriteLine(response.Content); }
            foreach(JObject blog in JObject.Parse(response.Content)["blogList"])
            {
                blogs.Add(new Objects.Blog(blog));
            }
            return blogs;
        }

        public List<Objects.Wiki> get_user_wikis(string userId, int start = 0, int size = 25)
        {
            List<Objects.Wiki> wikis = new List<Objects.Wiki>();
            RestClient client = new RestClient(helpers.BaseUrl);
            RestRequest request = new RestRequest($"/x{communityId}/s/item?type=user-all&start={start}&size={size}&cv=1.2&uid={userId}");
            request.AddHeaders(headers);
            var response = client.ExecuteGet(request);
            if ((int)response.StatusCode != 200) { throw new Exception(response.Content); }
            if (debug) { Trace.WriteLine(response.Content); }
            foreach(JObject wiki in JObject.Parse(response.Content)["itemList"])
            {
                wikis.Add(new Objects.Wiki(wiki));
            }
            return wikis;
        }

        public Objects.UserAchievements get_user_achievements(string userId)
        {
            RestClient client = new RestClient(helpers.BaseUrl);
            RestRequest request = new RestRequest($"/x{communityId}/s/user-profile/{userId}/achievements");
            request.AddHeaders(headers);
            var response = client.ExecuteGet(request);
            if ((int)response.StatusCode != 200) { throw new Exception(response.Content); }
            if (debug) { Trace.WriteLine(response.Content); }
            return new Objects.UserAchievements((JObject)JObject.Parse(response.Content)["achievements"]);
        }

        public Objects.InfluencerInfo get_influencer_info(string userId, int start = 0, int size = 25)
        {
            RestClient client = new RestClient(helpers.BaseUrl);
            RestRequest request = new RestRequest($"/x{communityId}/s/influencer/{userId}/fans?start={start}&size={size}");
            request.AddHeaders(headers);
            var response = client.ExecuteGet(request);
            if((int)response.StatusCode != 200) { throw new Exception(response.Content); }
            if(debug) {  Trace.WriteLine(response.Content); }
            return new(JObject.Parse(response.Content));
            
        }

        public Task add_influencer(string userId, int monthlyFee)
        {
            RestClient client = new RestClient(helpers.BaseUrl);
            RestRequest request = new RestRequest($"/x{communityId}/s/influencer/{userId}");
            request.AddHeaders(headers);
            JObject data = new JObject()
            {
                { "monthlyFee", monthlyFee },
                { "timestamp", helpers.GetTimestamp() * 1000 }
            };
            request.AddJsonBody(JsonConvert.SerializeObject(data));
            request.AddHeader("NDC-MSG-SIG", helpers.generate_signiture(JsonConvert.SerializeObject(data)));
            var response = client.ExecutePost(request);
            if ((int)response.StatusCode != 200) { throw new Exception(response.Content); }
            if (debug) { Trace.WriteLine(response.Content); }
            return Task.CompletedTask;
        }
        public Task remove_influencer(string userId)
        {
            RestClient client = new RestClient(helpers.BaseUrl);
            RestRequest request = new RestRequest($"/x{communityId}/s/influencer/{userId}");
            request.AddHeaders(headers);
            var response = client.Delete(request);
            if ((int)response.StatusCode != 200) { throw new Exception(response.Content); }
            if (debug) { Trace.WriteLine(response.Content); }
            return Task.CompletedTask;
        }

        public Task subscribe(string userId, bool autoRenew = false, string transactionId = null)
        {
            RestClient client = new RestClient(helpers.BaseUrl);
            RestRequest request = new RestRequest($"/x{communityId}/s/influencer/{userId}/subscribe");
            request.AddHeaders(headers);
            JObject data = new JObject()
            {
                { "paymentContext", new JObject()
                    {
                        { "transactionId", transactionId != null ? transactionId : helpers.generate_transaction_id() },
                        { "isAutoRenew", autoRenew }
                    }
                },
                { "timestamp", helpers.GetTimestamp() * 1000 }
            };
            request.AddJsonBody(JsonConvert.SerializeObject(data));
            request.AddHeader("NDC-MSG-SIG", helpers.generate_signiture(JsonConvert.SerializeObject(data)));
            var response = client.ExecutePost(request);
            if ((int)response.StatusCode != 200) { throw new Exception(response.Content); }
            if (debug) { Trace.WriteLine(response.Content); }
            return Task.CompletedTask;
        }

        public List<Objects.BlockedUser> get_blocked_users(int start = 0, int size = 25)
        {
            List<Objects.BlockedUser> blocked = new List<Objects.BlockedUser>();
            RestClient client = new RestClient(helpers.BaseUrl);
            RestRequest request = new RestRequest($"/x{communityId}/s/block?start={start}&size={size}");
            request.AddHeaders(headers);
            var response = client.ExecuteGet(request);
            if((int)response.StatusCode != 200) { throw new Exception(response.Content); }
            if (debug) { Trace.WriteLine(response.Content); }
            foreach(JObject user in JObject.Parse(response.Content)["userProfileList"])
            {
                blocked.Add(new(user));
            }
            return blocked;
        }

        public List<string> get_blocker_users(int start = 0, int size = 25)
        {
            List<string> blockers = new();
            RestClient client = new RestClient(helpers.BaseUrl);
            RestRequest request = new RestRequest($"/x{communityId}/s/block?start={start}&size={size}");
            request.AddHeaders(headers);
            var response = client.ExecuteGet(request);
            if ((int)response.StatusCode != 200) { throw new Exception(response.Content); }
            if(debug) { Trace.WriteLine(response.Content); }
            foreach(string blocker in JObject.Parse(response.Content)["blockerUidList"]) { blockers.Add(blocker); }
            return blockers;
        }

        public List<Objects.UserProfile> get_leaderboard_info(Types.Leaderboard_Ranking_Types type, int start = 0, int size = 25)
        {
            List<Objects.UserProfile> leaderboard = new();
            RestClient client = new RestClient(helpers.BaseUrl);
            RestRequest request = new RestRequest($"/g/s-x{communityId}/community/leaderboard?rankingType={(int)type}&start={start}&size={size}");
            request.AddHeaders(headers);
            var response = client.ExecuteGet(request);
            if ((int)response.StatusCode != 200) { throw new Exception(response.Content); }
            if(debug) { Trace.WriteLine(response.Content); }
            foreach(JObject user in JObject.Parse(response.Content)["userProfileList"]) { leaderboard.Add(new(user)); }
            return leaderboard;
        }


        public Objects.Wiki get_wiki_info(string wikiId)
        {
            RestClient client = new RestClient(helpers.BaseUrl);
            RestRequest request = new RestRequest($"/x{communityId}/s/item/{wikiId}");
            request.AddHeaders(headers);
            var response = client.ExecuteGet(request);
            if((int)response.StatusCode != 200) { throw new Exception(response.Content); }
            if(debug) { Trace.WriteLine(response.Content); }
            return new((JObject)JObject.Parse(response.Content)["item"]);
        }

        public Task kick_from_chat(string userId, string chatId, bool allowRejoin = true)
        {
            int _allowRejoin = allowRejoin ? 1 : 0;
            RestClient client = new RestClient(helpers.BaseUrl);
            RestRequest request = new RestRequest($"/x{communityId}/s/chat/thread/{chatId}/member/{userId}?allowRejoin={_allowRejoin}");
            request.AddHeaders(headers);

            var response = client.Delete(request);
            if((int)response.StatusCode != 200) { throw new Exception(response.Content); }
            if(debug) { Trace.WriteLine(response.Content); } 
            return Task.CompletedTask;
        }

        public Task delete_chat(string chatId)
        {
            RestClient client = new RestClient(helpers.BaseUrl);
            RestRequest request = new RestRequest($"/x{communityId}/s/chat/thread/{chatId}");
            request.AddHeaders(headers);
            var response = client.Delete(request);
            if ((int)response.StatusCode != 200) { throw new Exception(response.Content); }
            if (debug) { Trace.WriteLine(response.Content); }
            return Task.CompletedTask;
        }


        public Task handle_promotion(string noticeId, bool accept = true)
        {
            RestClient client = new RestClient(helpers.BaseUrl);
            string _type = accept ? "accept" : "decline";
            RestRequest request = new RestRequest($"/x{communityId}/s/notice/{noticeId}/{_type}");
            request.AddHeaders(headers);
            var response = client.ExecutePost(request);
            if((int)response.StatusCode != 200) { throw new Exception(response.Content); }
            if(debug) { Trace.WriteLine(response.Content); }
            return Task.CompletedTask;
        }

        public Task play_quiz(string quizId, List<string> questionIdList, List<string> answerIdList, int quizMode = 0)
        {
            RestClient client = new RestClient(helpers.BaseUrl);
            RestRequest request = new RestRequest($"/x{communityId}/s/blog/{quizId}/quiz/result");

            JArray quizData = new JArray();


            for (int i = 0; i < questionIdList.Count && i < answerIdList.Count; i++)
            {
                JObject part = new JObject
                {
                    { "optIdList", new JArray { answerIdList[i] } },
                    { "quizQuestionId", questionIdList[i] },
                    { "timeSpent", 0.0 }
                };

                quizData.Add(part);
            }


            JObject data = new JObject()
            {
                { "mode", quizMode },
                { "quizAnswerList", quizData },
                { "timestamp", helpers.GetTimestamp() * 1000 }
            };
            request.AddHeaders(headers);
            request.AddJsonBody(JsonConvert.SerializeObject(data));
            request.AddHeader("NDC-MSG-SIG", helpers.generate_signiture(JsonConvert.SerializeObject(data)));
            var response = client.ExecutePost(request);
            if((int)response.StatusCode != 200) { throw new Exception(response.Content); }
            if(debug) { Trace.WriteLine(response.Content); }
            return Task.CompletedTask;
        }



        /// <summary>
        /// Not to be used in general use (THIS FUNCTION WILL DISPOSE THE SUBCLIENT)
        /// </summary>
        public void Dispose()
        {
            this.communityId = null;
            this.client = null;
            this.headers = null;
        }


    }


}
