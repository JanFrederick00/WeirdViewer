using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;

namespace WeirdViewer
{
    public class WimpyScene
    {
        private GGDict dict;

        // Wimpy Data
        public string Name;
        public string Background;
        public string Sheet;
        public Color AmbientLight;
        public List<Polygon> WalkBoxes = new List<Polygon>();
        public List<SceneLayer> Layers = new List<SceneLayer>();
        public List<SceneLight> Lights = new List<SceneLight>();
        public List<SceneObject> Objects = new List<SceneObject>();

        public Vector2[] RoomBounds = new Vector2[2];
        public double RoomScale;
        public Vector2 RoomSize;


        public WimpyScene(byte[] wimpyFile)
        {
            dict = new GGDict(new MemoryStream(wimpyFile), true);
            var root = dict.Root;
            Name = (string)root["name"];
            Background = (string)root["background"];
            Sheet = (string)root["sheet"];
            AmbientLight = Color.FromArgb(255, 255, 255);
            if(root.ContainsKey("ambient_light")) AmbientLight = ParseColor((string)root["ambient_light"]);

            RoomBounds = ParseVectors((string)root["room_bounds"]);
            RoomSize = ParseVectors((string)root["room_size"])[0];
            if (root["room_scale"] is int rsi) RoomScale = rsi;
            else if (root["room_scale"] is double rsd) RoomScale = rsd;

            object[] walkBoxes = (object[])root["walkboxes"];
            foreach (var wbox in walkBoxes.Select(s => (Dictionary<string, object>)s))
            {
                string polygon = (string)wbox["polygon"];
                Polygon pgon = new Polygon();
                pgon.Points = polygon.Split(';').Select(s => ParseVectors(s)[0]).ToList();
                WalkBoxes.Add(pgon);
            }

            if (root.ContainsKey("layers"))
            {
                object[] layers = (object[])root["layers"];
                foreach (var layer in layers.Select(s => (Dictionary<string, object>)s))
                {
                    SceneLayer sl = new SceneLayer()
                    {
                        Name = (string)layer["name"],
                        ZSort = (int)layer["zsort"],
                    };
                    var parallax = layer["parallax"];
                    if (parallax is double pd) sl.Parallax = new Vector2((float)pd, (float)pd);
                    else if (parallax is string ps) sl.Parallax = ParseVectors(ps)[0];
                    Layers.Add(sl);
                }
            }

            if (root.ContainsKey("lights"))
            {
                object[] lights = (object[])root["lights"];
                foreach (var light in lights.Select(s => (Dictionary<string, object>)s))
                {
                    SceneLight sl = new SceneLight();
                    sl.Clipping = ParseVectors((string)light["clipping"]);
                    sl.color1 = ParseColor((string)light["color1"]);
                    sl.color2 = ParseColor((string)light["color2"]);
                    sl.id = (int)light["id"];
                    sl.iradius = (int)light["iradius"];
                    sl.oradius = (int)light["oradius"];
                    sl.pos = ParseVectors((string)light["pos"])[0];
                    if (light["rate"] is int iRate) sl.rate = iRate;
                    else sl.rate = (double)light["rate"];

                    Lights.Add(sl);
                }
            }

            object[] objects = (object[])root["objects"];
            foreach (var obj in objects.Select(s => (Dictionary<string, object>)s))
            {
                SceneObject so = new SceneObject();
                if (obj.ContainsKey("hotspot")) so.hotspot = ParseVectors((string)obj["hotspot"]);
                if (obj.ContainsKey("name")) so.name = (string)obj["name"];
                if (obj.ContainsKey("pos")) so.pos = ParseVectors((string)obj["pos"])[0];
                if (obj.ContainsKey("scale")) so.scale = ParseVectors((string)obj["scale"])[0];
                //if (obj.ContainsKey("shader")) so.shader = (string)obj["shader"];
                if (obj.ContainsKey("targetPos")) so.targetPos = ParseVectors((string)obj["targetPos"])[0];
                if (obj.ContainsKey("usedir")) so.usedir = (string)obj["usedir"];
                if (obj.ContainsKey("usepos")) so.usepos = ParseVectors((string)obj["usepos"])[0];
                if (obj.ContainsKey("zsort")) so.zsort = (int)obj["zsort"];
                if (obj.ContainsKey("prop")) so.prop = (int)obj["prop"];
                if (obj.ContainsKey("spot")) so.spot = (int)obj["spot"];
                if (obj.ContainsKey("emitter")) so.emitter = (int)obj["emitter"];
                if (obj.ContainsKey("emitterAutostart")) so.emitterAutostart = (int)obj["emitterAutostart"];
                if (obj.ContainsKey("emitterCountFactor")) so.emitterCountFactor = (int)obj["emitterCountFactor"];
                if (obj.ContainsKey("emitterLifetimeFactor")) so.emitterLifetimeFactor = (int)obj["emitterLifetimeFactor"];
                if (obj.ContainsKey("emitterName")) so.emitterName = (string)obj["emitterName"];
                if (obj.ContainsKey("emitterPrime")) so.emitterPrime = (int)obj["emitterPrime"];
                if (obj.ContainsKey("emitterType")) so.emitterType = (int)obj["emitterType"];
                if (obj.ContainsKey("spine")) so.spine = (int)obj["spine"];
                if (obj.ContainsKey("spinefile")) so.spinefile = (string)obj["spinefile"];
                if (obj.ContainsKey("animations"))
                {
                    object[] animations = (object[])obj["animations"];
                    foreach(var anim in animations.Select(s => (Dictionary<string, object>)s))
                    {
                        SceneObject.Animation animation = new SceneObject.Animation();
                        animation.name = (string)anim["name"];
                        if (anim.ContainsKey("frames")) animation.frames.AddRange(((object[])anim["frames"]).Select(s => (string)s));
                        if (anim.ContainsKey("loop")) animation.loop = (int)anim["loop"];
                        else animation.loop = 0;

                        so.animations.Add(animation);
                    }
                }
                Objects.Add(so);
            }
        }

        static Vector2[] ParseVectors(string vecs)
        {
            List<Vector2> v = new List<Vector2>();
            while (vecs.Contains("("))
            {
                int start = 0; int end = vecs.IndexOf(")");
                string currentVector = vecs.Substring(start, end);
                vecs = vecs.Substring(end + 1).TrimStart(',');

                currentVector = currentVector.Trim('(', ')');
                string[] numbers = currentVector.Split(',').Where(l => l.Length > 0).ToArray();
                if (numbers.Length == 0) continue;
                if (numbers.Length != 2) throw new InvalidDataException("Non-2d Vector?");
                double x = double.Parse(numbers[0]);
                double y = double.Parse(numbers[1]);
                v.Add(new Vector2((float)x, (float)y));
            }
            return v.ToArray();
        }

        static Color ParseColor(string col)
        {
            col = col.Trim('{', '}');
            string[] components = col.Split(',');
            if (components.Length != 3 && components.Length != 4) throw new InvalidDataException("Unknown color value");
            float r, g, b, a;
            r = float.Parse(components[0]);
            g = float.Parse(components[1]);
            b = float.Parse(components[2]);
            if (components.Length == 4)
            {
                a = float.Parse(components[3]);
            }
            else a = 1;
            return Color.FromArgb((byte)(a * 255), (byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
        }

        public class SceneObject
        {
            public List<Animation> animations = new List<Animation>();
            public Vector2[] hotspot = new Vector2[4];
            public string name;
            public Vector2 pos;
            public Vector2 scale;
            public string shader = "";
            public Vector2 targetPos;
            public string usedir;
            public Vector2 usepos;
            public int zsort;
            public int prop = 0;
            public int spot = 0;
            public int spine = 0;
            public string spinefile = "";

            public int emitter = 0;
            public int emitterAutostart;
            public int emitterCountFactor;
            public int emitterLifetimeFactor;
            public string emitterName;
            public int emitterPrime;
            public int emitterType;

            public class Animation
            {
                public List<string> frames = new List<string>();
                public string name;
                public int loop;
            }
        }

        public class SceneLight
        {
            public Vector2[] Clipping = new Vector2[2];
            public Color color1;
            public Color color2;
            public int id;
            public int iradius;
            public int oradius;
            public Vector2 pos;
            public double rate;
        }

        public class SceneLayer
        {
            public int ZSort;
            public Vector2 Parallax;
            public string Name;
        }

        public class Polygon
        {
            public List<Vector2> Points = new List<Vector2>();
        }


    }
}
