using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SSAANIP{
    public class RequestMethods{ //different requests
        public string username;
        public string password;
        public RequestMethods(string username, string password)
        {
            this.username = username;
            this.password = password;
        }
        public async Task<IEnumerable<XElement>> System(string requestType){
            Request request = new(username, password, requestType);
            return await request.sendRequestAsync();
            
        }
        public async Task<IEnumerable<XElement>> Browsing(string requestType, string id){
            if (id != null){
                id = $"&id={id}";
            }
            Request request = new(username,password, requestType, id);
            return await request.sendRequestAsync();
        }
        public async Task<IEnumerable<XElement>> sendCreatePlaylist(string name, string[] songId){
            string extras = $"&name={name}";
            foreach (string sID in songId){
                extras += $"&songID={sID}";
            }
            Request request = new(username, password, "createPlaylist", extras);
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
            Request request = new(username, password, "updatePlaylist", extras);
            return await request.sendRequestAsync();
        }
        public async Task<IEnumerable<XElement>> sendGetIndexes(){
            Request request = new(username, password, "getIndexes");
            IEnumerable<XElement> output = await request.sendRequestAsync();
            return output;
        }
        public async Task<IEnumerable<XElement>> sendGetAlbum(string id)
        {
            Request request = new(username, password, "getAlbum", "&id="+id);
            IEnumerable<XElement> output = await request.sendRequestAsync();
            return output;
        }
        public async Task<IEnumerable<XElement>> sendGetArtist(string id)
        {
            Request request = new(username, password, "getArtist", "&id=" + id);
            IEnumerable<XElement> output = await request.sendRequestAsync();
            return output;
        }
    }
}