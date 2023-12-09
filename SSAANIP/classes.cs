using System;
using System.Collections.Generic;

namespace SSAANIP{
    public class artist {
        private string id;
        private string name;
        private List<album> albums;
        public artist(string id, string name){
            this.id = id;
            this.name = name;
        }
        public void addAlbums(album newAlbum){
            albums.Add(newAlbum);
        }
        public string returnId() { return id; }
        public string returnName() { return name; }
        public List<album> GetAlbums() { return albums; }
    }
    public class album {
        private string id;
        private string name;
        private List<track> tracks;
        public album(string id, string name){
            this.id = id;
            this.name = name;
        }
        public void addTracks(track newTrack){
            tracks.Add(newTrack);
        }
        public string returnId() { return id; }
        public string returnName() { return name; }
        public List<track> getTracks() {  return tracks; }
    }
    public class track{
        private string id;
        private string name;

        public track(string id, string name){
            this.id=id;
            this.name = name;
        }
        public string returnName() { return name; }
        public string returnId() { return id; }
    }
}