﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Amino.Objects
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class YouTubeMessage
    {
        public int _type { get; }
        public int alertOption { get; }
        public int membershipStatus { get; }
        public int communityId { get; }

        public string mediaValue { get; }
        public string chatId { get; }
        public int mediaType { get; }
        public string videoTitle { get; }
        public int clientRefId { get; }
        public string messageId { get; }
        public string userId { get; }
        public string createdTime { get; }
        public int type { get; }
        public bool isHidden { get; }
        public bool includedInSummary { get; }
        public string chatBubbleId { get; }
        public int chatBubbleVersion { get; }
        public string json { get; }
        public _Author Author { get; }


        public YouTubeMessage(JObject _json)
        {
            dynamic jsonObj = (JObject)JsonConvert.DeserializeObject(_json.ToString());
            try { _type = (int)jsonObj["t"]; } catch { }
            try { communityId = (int)jsonObj["o"]["ndcId"]; } catch { }
            try { alertOption = (int)jsonObj["o"]["alertOption"]; } catch { }
            try { membershipStatus = (int)jsonObj["o"]["membershipStatus"]; } catch { }
            try { mediaValue = (string)jsonObj["o"]["chatMessage"]["mediaValue"]; } catch { }
            try { chatId = (string)jsonObj["o"]["chatMessage"]["threadId"]; } catch { }
            try { videoTitle = (string)jsonObj["o"]["chatMessage"]["content"]; } catch { }
            try { clientRefId = (int)jsonObj["o"]["chatMessage"]["clientRefId"]; } catch { }
            try { messageId = (string)jsonObj["o"]["chatMessage"]["messageId"]; } catch { }
            try { userId = (string)jsonObj["o"]["chatMessage"]["userId"]; } catch { }
            try { createdTime = (string)jsonObj["o"]["chatMessage"]["createdTime"]; } catch { }
            try { type = (int)jsonObj["o"]["chatMessage"]["type"]; } catch { }
            try { isHidden = (bool)jsonObj["o"]["chatMessage"]["isHidden"]; } catch { }
            try { includedInSummary = (bool)jsonObj["o"]["chatMessage"]["includedInSummary"]; } catch { }
            try { chatBubbleId = (string)jsonObj["o"]["chatMessage"]["chatBubbleId"]; } catch { }
            try { chatBubbleVersion = (int)jsonObj["o"]["chatMessage"]["chatBubbleVersion"]; } catch { }
            Author = new _Author(_json);
            json = _json.ToString();

        }

        [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
        public class _Author
        {
            public string userId { get; }
            public int status { get; }
            public string iconUrl { get; }
            public int reputationn { get; }
            public int role { get; }
            public string nickname { get; }
            public int level { get; }
            public int accountMembershipStatus { get; }
            public _AvatarFrame AvatarFrame { get; }
            public _InfluencerInfo InfluencerInfo { get; }

            public _Author(JObject _json)
            {
                dynamic jsonObj = (JObject)JsonConvert.DeserializeObject(_json.ToString());
                try { userId = (string)jsonObj["o"]["chatMessage"]["author"]["uid"]; } catch { }
                try { status = (int)jsonObj["o"]["chatMessage"]["author"]["status"]; } catch { }
                try { if (jsonObj["o"]["chatMessage"]["author"]["icon"] != null) { iconUrl = (string)jsonObj["o"]["chatMessage"]["author"]["icon"]; } } catch { }
                try { reputationn = (int)jsonObj["o"]["chatMessage"]["author"]["reputation"]; } catch { }
                try { role = (int)jsonObj["o"]["chatMessage"]["author"]["role"]; } catch { }
                try { nickname = (string)jsonObj["o"]["chatMessage"]["author"]["nickname"]; } catch { }
                try { level = (int)jsonObj["o"]["chatMessage"]["author"]["level"]; } catch { }
                try { accountMembershipStatus = (int)jsonObj["o"]["chatMessage"]["author"]["accountMembershipStatus"]; } catch { }
                try { if (jsonObj["o"]["chatMessage"]["author"]["avatarFrame"] != null) { AvatarFrame = new _AvatarFrame(_json); } } catch { }
                try { if (jsonObj["o"]["chatMessage"]["author"]["influencerInfo"] != null) { InfluencerInfo = new _InfluencerInfo(_json); } } catch { }
            }

            [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
            public class _AvatarFrame
            {
                public int status { get; }
                public int version { get; }
                public string resourceUrl { get; }
                public string name { get; }
                public string iconUrl { get; }
                public int frameType { get; }
                public string frameId { get; }

                public _AvatarFrame(JObject _json)
                {
                    dynamic jsonObj = (JObject)JsonConvert.DeserializeObject(_json.ToString());
                    try { status = (int)jsonObj["o"]["chatMessage"]["author"]["avatarFrame"]["status"]; } catch { }
                    try { version = (int)jsonObj["o"]["chatMessage"]["author"]["avatarFrame"]["version"]; } catch { }
                    try { resourceUrl = (string)jsonObj["o"]["chatMessage"]["author"]["avatarFrame"]["resourceUrl"]; } catch { }
                    try { name = (string)jsonObj["o"]["chatMessage"]["author"]["avatarFrame"]["name"]; } catch { }
                    try { iconUrl = (string)jsonObj["o"]["chatMessage"]["author"]["avatarFrame"]["icon"]; } catch { }
                    try { frameType = (int)jsonObj["o"]["chatMessage"]["author"]["avatarFrame"]["frameType"]; } catch { }
                    try { frameId = (string)jsonObj["o"]["chatMessage"]["author"]["avatarFrame"]["frameId"]; } catch { }
                }
            }

            [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
            public class _InfluencerInfo
            {
                public int fansCount { get; }
                public int monthlyFee { get; }

                public _InfluencerInfo(JObject _json)
                {
                    dynamic jsonObj = (JObject)JsonConvert.DeserializeObject(_json.ToString());
                    try { fansCount = (int)jsonObj["o"]["chatMessage"]["author"]["influencerInfo"]["fansCount"]; } catch { }
                    try { monthlyFee = (int)jsonObj["o"]["chatMessage"]["author"]["influencerInfo"]["monthlyFee"]; } catch { }
                }
            }
        }
    }
}
