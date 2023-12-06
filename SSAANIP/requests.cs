using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SSAANIP
{
    public class RequestMethods //different requests
    {
        public string socket;
        public string username;
        public string version;
        public string appName;

        public RequestMethods(string socket, string username, string version, string appName)
        {
            this.socket = socket;
            this.username = username;
            this.version = version;
            this.appName = appName;

        }
        public async Task<IEnumerable<XElement>> System(string requestType)
        {
            Request request = new(socket, username, version, requestType, appName);
            return await request.sendRequestAsync();
            
        }
        public async Task<IEnumerable<XElement>> Browsing(string requestType, string id)
        {
            if (id != null)
            {
                id = $"&id={id}";
            }
            Request request = new(socket, username, version, requestType, appName, id);
            return await request.sendRequestAsync();
        }
        public async Task<IEnumerable<XElement>> sendCreatePlaylist(string name, string[] songId)
        {
            string extras = $"&name={name}";
            foreach (string sID in songId)
            {
                extras += $"&songID={sID}";
            }
            Request request = new(socket, username, version, "createPlaylist", appName, extras);
            return await request.sendRequestAsync();
        }
        public async Task<IEnumerable<XElement>> sendUpdatePlaylist(string playlistID, string name, string comment, string isPublic, string[] songID, string[] songIndex){
            string extras = string.Empty;
            if (name != null){
                extras += $"&name={name}";
            }
            else if (comment != null){
                extras += $"&comment={comment}";
            }
            Request request = new(socket, username, version, "updatePlaylist", appName, extras);
            return await request.sendRequestAsync();
        }
        public async Task<IEnumerable<XElement>> sendGetIndexes(){
            Request request = new(socket, username, version, "getIndexes", appName);
            IEnumerable<XElement> output = await request.sendRequestAsync();
            return output;

        }
        public async Task<IEnumerable<XElement>> sendGetUser(string username){
            Request request = new(socket,username, version, "getUser", appName);
            IEnumerable<XElement> output = await request.sendRequestAsync();
            return output;
        }

    }
}