using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LittleRoad
{
    // Original reusable architectural modules. World positions are in metres;
    // frontages face +Z and the garage bay is genuinely open and driveable.
    public sealed class VillageKit
    {
        readonly Transform root;
        readonly Transform plantingRoot;
        readonly CoastalVegetation planting;
        readonly Material plaster,cream,stone,roof,roofLight,wood,teal,dark,glass,metal;
        public VillageKit()
        {
            root=new GameObject("Bayview village / original architecture").transform;
            plantingRoot=new GameObject("Bayview village / shared planting").transform;
            planting=new CoastalVegetation();
            plaster=Mat("Warm lime plaster",new Color(.86f,.77f,.60f));cream=Mat("Fresh garage plaster",new Color(.90f,.87f,.75f));
            stone=Mat("Honey limestone",new Color(.61f,.57f,.46f));roof=Mat("Fired terracotta",new Color(.54f,.23f,.12f));roofLight=Mat("Sunlit roof tiles",new Color(.68f,.33f,.18f));
            wood=Mat("Weathered oak",new Color(.34f,.24f,.14f));teal=Mat("Painted teal joinery",new Color(.12f,.32f,.32f));dark=Mat("Workshop charcoal",new Color(.055f,.09f,.10f));
            glass=TrialWorld.Material("Blue window reflection",new Color(.22f,.38f,.41f),.08f,.6f);
            metal=TrialWorld.Material("Workshop steel",new Color(.37f,.41f,.42f),.65f,.35f);
        }
        Material Mat(string name,Color color)
        {
            var material=TrialWorld.Material(name,color,0,.15f);
            // Original small-scale mottling keeps broad plaster/stone faces
            // from reading as unshaded solid-colour blocks at driving distance.
            var texture=new Texture2D(64,64,TextureFormat.RGB24,true);texture.name=name+" / surface grain";
            var pixels=new Color[64*64];
            for(int y=0;y<64;y++)for(int x=0;x<64;x++) {
                float grain=.94f+.06f*Mathf.PerlinNoise(x*.17f,y*.17f)+.025f*Mathf.PerlinNoise(x*.71f,y*.71f);
                pixels[y*64+x]=new Color(grain,grain,grain);
            }
            texture.SetPixels(pixels);texture.Apply();texture.wrapMode=TextureWrapMode.Repeat;
            material.mainTexture=texture;material.mainTextureScale=new Vector2(3,3);return material;
        }
        GameObject Piece(string name,Vector3 p,Vector3 size,Material material,bool solid=false,PrimitiveType shape=PrimitiveType.Cube)
        {
            var go=GameObject.CreatePrimitive(shape);go.name=name;go.transform.SetParent(root);go.transform.position=p;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=material;
            var collider=go.GetComponent<Collider>();
            if(!solid){collider.enabled=false;Object.Destroy(collider);}else {
                go.tag="Respawn";
                if(shape!=PrimitiveType.Cube){collider.enabled=false;Object.Destroy(collider);go.AddComponent<MeshCollider>().sharedMesh=go.GetComponent<MeshFilter>().sharedMesh;}
            }
            return go;
        }
        void Beam(string name,Vector3 a,Vector3 b,float width,Material material,bool solid=false)
        {
            var beam=Piece(name,(a+b)*.5f,new Vector3(width,width,Vector3.Distance(a,b)),material,solid);beam.transform.rotation=Quaternion.LookRotation(b-a);
        }
        void Sign(string words,Vector3 p,float scale,Color color)
        {
            var go=new GameObject("Sign / "+words.Replace('\n',' '));go.transform.SetParent(root);go.transform.position=p;go.transform.rotation=Quaternion.Euler(0,180,0);
            var text=go.AddComponent<TextMesh>();text.text=words;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.fontSize=80;text.characterSize=scale;text.color=color;
            text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontStyle=FontStyle.Bold;go.GetComponent<Renderer>().sharedMaterial=text.font.material;
            go.AddComponent<WorldText>();
        }
        void Gable(Vector3 p,float width,float depth,float eaves,float rise,Material material)
        {
            // Front/back triangles and side walls cap the volume beneath the roof.
            var vertices=new[]{new Vector3(-width/2,eaves,depth/2),new Vector3(width/2,eaves,depth/2),new Vector3(0,eaves+rise,depth/2),
                new Vector3(-width/2,eaves,-depth/2),new Vector3(width/2,eaves,-depth/2),new Vector3(0,eaves+rise,-depth/2)};
            var mesh=new Mesh{name="Plaster gable"};mesh.vertices=vertices;mesh.triangles=new[]{0,1,2,3,5,4,0,5,3,0,2,5,1,5,2,1,4,5};mesh.RecalculateNormals();
            var go=new GameObject("Gable infill",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root);go.transform.position=p;go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<Renderer>().sharedMaterial=material;
            float half=width*.5f+.45f,length=Mathf.Sqrt(half*half+rise*rise),slope=Mathf.Atan2(rise,half)*Mathf.Rad2Deg;
            foreach(int side in new[]{-1,1}) {
                var baseRoof=Piece("Pitched roof plane",p+new Vector3(side*half*.5f,eaves+rise*.5f,0),new Vector3(length,.18f,depth+1.1f),roof);
                baseRoof.transform.rotation=Quaternion.Euler(0,0,-side*slope);
                int rows=Mathf.CeilToInt(length/.48f),columns=Mathf.CeilToInt((depth+1)/.55f);
                for(int row=0;row<rows;row++)for(int col=0;col<columns;col++) {
                    float t=(row+.5f)/rows;float x=side*half*t,y=eaves+rise*(1-t)+.115f,z=-(depth+1)*.5f+(col+.5f)*(depth+1)/columns;
                    var tile=Piece("Overlapping clay tile",p+new Vector3(x,y,z),new Vector3(length/rows+.06f,.085f,(depth+1)/columns-.025f),(row+col)%4==0?roofLight:roof);
                    tile.transform.rotation=Quaternion.Euler(0,0,-side*slope);
                }
            }
            Beam("Ridge cap",p+new Vector3(0,eaves+rise+.17f,-depth*.5f-.6f),p+new Vector3(0,eaves+rise+.17f,depth*.5f+.6f),.27f,roofLight);
            foreach(int side in new[]{-1,1})Beam("Front roof fascia",p+new Vector3(side*half,eaves,depth*.5f+.6f),p+new Vector3(0,eaves+rise,depth*.5f+.6f),.18f,cream);
        }
        void Window(Vector3 p,float width=1.3f,float height=1.7f,bool shutters=true)
        {
            Piece("Window reveal",p,new Vector3(width+.24f,height+.24f,.14f),stone);
            Piece("Window glazing",p+Vector3.forward*.09f,new Vector3(width,height,.035f),glass);
            Piece("Window transom",p+Vector3.forward*.13f,new Vector3(width,.07f,.08f),cream);
            Piece("Window mullion",p+Vector3.forward*.13f,new Vector3(.07f,height,.08f),cream);
            Piece("Limestone window sill",p+new Vector3(0,-height*.5f-.11f,.18f),new Vector3(width+.45f,.16f,.42f),cream);
            if(shutters)foreach(int side in new[]{-1,1}) {
                float x=side*(width*.5f+.36f);Piece("Open shutter",p+new Vector3(x,0,.06f),new Vector3(.48f,height+.1f,.10f),teal);
                for(int i=0;i<7;i++)Piece("Shutter slat",p+new Vector3(x,-height*.4f+i*height*.13f,.13f),new Vector3(.44f,.045f,.05f),wood);
            }
        }
        void Planter(Vector3 p,float width=2.2f)
        {
            Piece("Stone planter",p+Vector3.up*.32f,new Vector3(width,.64f,.9f),stone,true);
            Piece("Planter earth",p+Vector3.up*.65f,new Vector3(width-.16f,.035f,.73f),wood);
            foreach(int side in new[]{-1,1}) {
                Piece("Planter limestone rim",p+new Vector3(0,.67f,side*.43f),new Vector3(width+.12f,.11f,.14f),cream);
                Piece("Planter limestone end",p+new Vector3(side*width*.5f,.67f,0),new Vector3(.12f,.11f,.75f),cream);
            }
            int count=Mathf.CeilToInt(width/.55f);
            for(int i=0;i<count;i++) {
                Vector3 at=p+new Vector3(Mathf.Lerp(-width*.34f,width*.34f,(i+.5f)/count),.67f,(i%2-.5f)*.13f);
                planting.PlantScrub(plantingRoot,at,.44f+(i%2)*.06f,1+i%2,i*137+p.x*7,true);
            }
        }
        void Awning(Vector3 p,float width)
        {
            for(int i=0;i<10;i++) {
                var strip=Piece("Canvas awning stripe",p+new Vector3(-width*.5f+(i+.5f)*width/10,-.22f,.68f),new Vector3(width/10+.006f,.06f,1.65f),i%2==0?cream:teal);
                strip.transform.rotation=Quaternion.Euler(15,0,0);
                Piece("Awning valance",p+new Vector3(-width*.5f+(i+.5f)*width/10,-.47f,1.44f),new Vector3(width/10,.25f,.06f),i%2==0?cream:teal);
            }
            foreach(int side in new[]{-1,1})Beam("Awning bracket",p+new Vector3(side*width*.47f,-1,0),p+new Vector3(side*width*.47f,-.45f,1.45f),.065f,metal);
        }
        void House(Vector3 p,float width,float depth,float height,string shop,bool awning)
        {
            Piece("Limewashed village house",p+Vector3.up*height*.5f,new Vector3(width,height,depth),plaster,true);
            Piece("Stone plinth",p+Vector3.up*.35f,new Vector3(width+.13f,.7f,depth+.13f),stone);
            Gable(p,width,depth,height,2.2f,plaster);
            float front=depth*.5f+.10f;
            Piece("Recessed timber door",p+new Vector3(-width*.27f,1.35f,front),new Vector3(1.5f,2.7f,.15f),teal);
            Piece("Door frame top",p+new Vector3(-width*.27f,2.79f,front+.08f),new Vector3(1.78f,.16f,.20f),cream);
            Piece("Door handle",p+new Vector3(-width*.27f+.5f,1.25f,front+.12f),new Vector3(.06f,.27f,.08f),metal);
            Window(p+new Vector3(width*.22f,1.7f,front),2.1f,2.1f,false);
            foreach(int side in new[]{-1,1})Window(p+new Vector3(side*width*.25f,height-1.4f,front));
            Piece("Shop fascia",p+new Vector3(0,3.5f,front+.15f),new Vector3(width-.45f,.8f,.16f),teal);
            Sign(shop,p+new Vector3(0,3.52f,front+.25f),.055f,new Color(1,.94f,.77f));
            if(awning)Awning(p+new Vector3(0,3.03f,front+.15f),width-.6f);
            Piece("Chimney",p+new Vector3(width*.28f,height+1.6f,-depth*.24f),new Vector3(.85f,2.6f,.95f),stone);
            Piece("Chimney cap",p+new Vector3(width*.28f,height+2.93f,-depth*.24f),new Vector3(1.12f,.18f,1.2f),cream);
            Planter(p+new Vector3(width*.36f,0,front+1.4f),1.6f);
        }
        void Garage(Vector3 p)
        {
            // Clear bay: 7m wide x 4.1m high. No monolithic hidden box across its entrance.
            Piece("Garage left wall",p+new Vector3(-5,2.25f,0),new Vector3(2,4.5f,10),cream,true);
            Piece("Garage right wall",p+new Vector3(5,2.25f,0),new Vector3(2,4.5f,10),cream,true);
            Piece("Garage rear wall",p+new Vector3(0,2.25f,-4.8f),new Vector3(8,4.5f,.4f),cream,true);
            Piece("Bay lintel",p+new Vector3(0,4.4f,4.8f),new Vector3(8,.6f,.5f),cream,true);
            Gable(p,12,10,4.5f,2.8f,cream);
            foreach(int side in new[]{-1,1}) {
                Piece("Bay stone pier",p+new Vector3(side*4.0f,2,5.12f),new Vector3(.46f,4,.48f),stone,true);
                for(int row=0;row<8;row++)Piece("Pier masonry joint",p+new Vector3(side*4, .25f+row*.5f,5.37f),new Vector3(.47f,.023f,.025f),cream);
            }
            Sign("HARBOUR GARAGE",p+new Vector3(0,5.55f,5.14f),.075f,new Color(.055f,.09f,.12f));
            Sign("SERVICE  /  PARTS  /  ROAD READY",p+new Vector3(0,4.86f,5.16f),.035f,new Color(.11f,.15f,.16f));
            // A workshop ceiling and rafters close the interior roof volume.
            Piece("Workshop ceiling",p+new Vector3(0,4.13f,-.2f),new Vector3(8,.12f,9.4f),cream);
            for(int i=0;i<4;i++)Piece("Workshop ceiling joist",p+new Vector3(0,3.98f,-3.5f+i*2.2f),new Vector3(8,.22f,.20f),wood);
            Piece("Service floor",p+new Vector3(0,.015f,0),new Vector3(8,.03f,10),stone);
            Piece("Workshop workbench",p+new Vector3(0,1.15f,-3.9f),new Vector3(6,.18f,1.1f),wood,true);
            foreach(int side in new[]{-1,1})Piece("Bench legs",p+new Vector3(side*2.5f,.55f,-3.9f),new Vector3(.18f,1.1f,.7f),dark,true);
            Piece("Tool board",p+new Vector3(0,2.4f,-4.5f),new Vector3(5.5f,1.7f,.15f),wood);
            for(int i=0;i<8;i++) {
                Vector3 tool=p+new Vector3(-2.3f+i*.65f,2.4f,-4.38f);
                Piece("Hanging wrench",tool,new Vector3(.10f,.60f,.07f),metal);
                Piece("Wrench head",tool+Vector3.up*.27f,new Vector3(.25f,.15f,.07f),metal);
            }
            for(int i=0;i<3;i++) {
                Piece("Workshop shelf",p+new Vector3(3.5f,1+i*.8f,-1.8f),new Vector3(.8f,.12f,3),wood,true);
                for(int j=0;j<3;j++)Piece("Workshop parts box",p+new Vector3(3.5f,1.22f+i*.8f,-2.8f+j*.9f),new Vector3(.55f,.35f,.65f),j%2==0?teal:roofLight);
            }
            for(int i=0;i<3;i++) {
                var tyre=Piece("Stacked spare tyre",p+new Vector3(-3.0f,.22f+i*.4f,-2.7f),new Vector3(.86f,.20f,.86f),dark,true,PrimitiveType.Cylinder);
                Piece("Spare wheel hub",tyre.transform.position+Vector3.up*.201f,new Vector3(.35f,.008f,.35f),metal,false,PrimitiveType.Cylinder);
            }
            var light=new GameObject("Warm workshop practical").AddComponent<Light>();light.transform.SetParent(root);light.type=LightType.Point;light.color=new Color(1,.80f,.53f);light.intensity=3;light.range=11;light.transform.position=p+new Vector3(0,3.7f,0);
            Piece("Workshop light strip",p+new Vector3(0,3.85f,0),new Vector3(4,.09f,.18f),cream);
            Planter(p+new Vector3(-6.8f,0,5.4f));Planter(p+new Vector3(6.8f,0,5.4f));
            // Painted service-bay guides and a compact sign give the entrance
            // a purpose while leaving its central driving corridor clear.
            foreach(int side in new[]{-1,1})Piece("Service bay paint",p+new Vector3(side*1.8f,.037f,1),new Vector3(.10f,.012f,6.8f),cream);
            Piece("Garage notice board",p+new Vector3(-5,2.1f,5.13f),new Vector3(1.35f,1.8f,.14f),teal);
            Sign("KEEP\nEXPLORING",p+new Vector3(-5,2.1f,5.23f),.03f,new Color(1,.94f,.77f));
        }
        // Toy-town high street from the Blender kit: painted frontages facing the road (+Z),
        // shop signs, lamps, benches and a fountain. Everything stays over 12 m from the road
        // centreline (z about -88 here): the race shoulder must remain clear.
        void Street()
        {
            BayviewKit.Tint T(int wall,int shutter,int awning,int door)=>new(Toy.Hex(wall),Toy.Hex(shutter),Toy.Hex(awning),Toy.Hex(door));
            BayviewKit.Spawn("cafe",new Vector3(-49,0,-110),0,root,T(0xe89a6a,0x2bb5a8,0xff4d5e,0x1d6fa8),"THE DAILY MILE");
            BayviewKit.Spawn("post_office",new Vector3(-36,0,-110),0,root,T(0xe8c24e,0x3a7bd5,0x3a7bd5,0xd94040),"POST OFFICE");
            BayviewKit.Spawn("shop",new Vector3(-23,0,-110),0,root,T(0x8fc4ad,0x1f8a70,0x1fa37a,0x1f6b8a),"TIDES & TYRES");
            BayviewKit.Spawn("house_b",new Vector3(-12,0,-110.5f),0,root,T(0xd98b8b,0x2e9e5b,0xff4d5e,0x2e7bd6));
            BayviewKit.Spawn("shop",new Vector3(-1,0,-111),0,root,T(0x7fa9cf,0xff7ac8,0xff7ac8,0x315fbf),"BAYVIEW PAINTS");
            BayviewKit.Spawn("house_a",new Vector3(12,0,-112),0,root,T(0xece0c4,0x3a7bd5,0xff4d5e,0x3a7bd5));
            BayviewKit.Spawn("house_a",new Vector3(25,0,-117),-12,root,T(0xdc9562,0x2bb5a8,0xff4d5e,0xc0392b));
            BayviewKit.Spawn("fountain",new Vector3(-29.5f,0,-103.2f),0,root);
            foreach(float x in new[]{-44f,-16.5f,-6f,6f})BayviewKit.Spawn("lamp_post",new Vector3(x,0,-103.6f),0,root,T(0xfff3dc,0x1d6fa8,0xff4d5e,0x1d6fa8));
            foreach(float x in new[]{-35f,-24f,5f})BayviewKit.Spawn("bench",new Vector3(x,0,-103.4f),180,root);
            foreach(var (x,z) in new[]{(-80f,-104f),(-5.5f,-106f),(19f,-107f),(36f,-119f)})BayviewKit.Spawn("tree_round",new Vector3(x,0,z),x*7,root);
            foreach(var (x,z) in new[]{(-42f,-117f),(-17f,-117f),(4f,-118f)})BayviewKit.Spawn("tree_cypress",new Vector3(x,0,z),0,root);
            foreach(var (x,z) in new[]{(-74f,-117f),(-52f,-120f),(-30f,-119f),(-7f,-120f),(10f,-122f)})BayviewKit.Spawn("bush",new Vector3(x,0,z),x*13,root);
        }
        // Sea wall along the pad's southern edge, promenade railing (its colliders keep cars on
        // the pad), stairs down to a pier and bobbing fishing boats in the basin below.
        void Harbour()
        {
            const float face=-134.4f,water=CoastalLandscape.SeaLevel;
            var rail=new BayviewKit.Tint(Color.white,Toy.Hex(0x1d6fa8),Color.white,Color.white);
            for(float x=-80;x<=20;x+=12)BayviewKit.Spawn("sea_wall",new Vector3(x,0,face),180,root);
            for(float x=-83;x<=23;x+=6)BayviewKit.Spawn("promenade_rail",new Vector3(x,.27f,face+.2f),0,root,rail);
            BayviewKit.Spawn("harbour_steps",new Vector3(-44,0,face),180,root,rail);
            BayviewKit.Spawn("pier",new Vector3(-30,water+1.0f,face-.6f),180,root,rail);
            foreach(var (x,z,yaw,hull,stripe) in new[]{(-24.5f,-143f,172f,0xffffff,0xff4d5e),(-36f,-147f,8f,0x3a7bd5,0xffd23f),(-17f,-154f,160f,0xffd23f,0x1d6fa8)}) {
                var boat=BayviewKit.Spawn("boat",new Vector3(x,water,z),yaw,BayviewKit.AnimatedRoot,new BayviewKit.Tint(Toy.Hex(hull),Toy.Hex(0x2bb5a8),Toy.Hex(stripe),Color.white),solid:false);
                boat.AddComponent<KitBob>().phase=x;
            }
            foreach(float x in new[]{-70f,-52f,-14f,4f,18f})BayviewKit.Spawn("palm",new Vector3(x,0,-129.5f),x*11,root);
            foreach(float x in new[]{-62f,-22f,10f})BayviewKit.Spawn("bench",new Vector3(x,0,-131.2f),0,root);
            foreach(var (x,t) in new[]{(26f,0xff7ac8),(29.2f,0x5ec8ff),(32.4f,0xffd23f)})
                BayviewKit.Spawn("beach_hut",new Vector3(x,0,-126f),0,root,new BayviewKit.Tint(Toy.Hex(t),Color.white,Color.white,Toy.Hex(0x1d6fa8)));
            BayviewKit.Spawn("parasol",new Vector3(21f,0,-128f),30,root,new BayviewKit.Tint(Toy.Hex(0xffd23f),Toy.Hex(0xff4d5e),Toy.Hex(0xff4d5e),Color.white));
        }
        // Village green behind the shops (ice-cream kiosk, flower beds, picnic tables) and a
        // small car park east of the high street, well clear of the race shoulder.
        void GreenAndCarPark()
        {
            BayviewKit.Spawn("kiosk",new Vector3(-33,0,-123.5f),180,root,new BayviewKit.Tint(Toy.Hex(0xffc8d2),Color.white,Toy.Hex(0xff7ac8),Color.white));
            foreach(float x in new[]{-50f,-13f})BayviewKit.Spawn("flower_bed",new Vector3(x,0,-125f),x*9,root);
            foreach(float x in new[]{-41f,-25f})BayviewKit.Spawn("picnic_table",new Vector3(x,0,-126f),90,root);
            var paint=TrialWorld.Material("Car park paint",new Color(.97f,.96f,.9f));
            Piece("Car park tarmac",new Vector3(19.4f,.01f,-101.5f),new Vector3(19.5f,.016f,7),TrialWorld.Material("Car park tarmac",new Color(.30f,.32f,.35f)));
            string[] parked={"pebble","marlow","bramble","heron"};Color[] paints={Toy.Hex(0xff7ac8),Toy.Hex(0x5ec8ff),Toy.Hex(0x3ee6a4),Toy.Hex(0xffb400)};
            for(int i=0;i<5;i++) {
                float x=13+i*3.6f;Piece("Bay line",new Vector3(x-1.8f,.02f,-101.5f),new Vector3(.12f,.012f,5),paint);
                if(i==4)continue;
                var car=TrialWorld.Car(parked[i],paints[i]);car.name="Parked "+parked[i];car.transform.SetPositionAndRotation(new Vector3(x,.02f,-101.5f),Quaternion.Euler(0,i%2==0?180:0,0));
                var box=car.AddComponent<BoxCollider>();box.center=CarBodies.Envelope.center;box.size=CarBodies.Envelope.size;car.tag="Respawn";
                car.AddComponent<ResidentIdle>();var chatter=car.AddComponent<NpcChatter>();chatter.speaker=new[]{"Wren","Monty","Priya","Gus"}[i];chatter.title="Bayview car park";chatter.colour=paints[i];
            }
        }
        void LegacyStreet()
        {
            House(new Vector3(-49,0,-109),9,9,6.5f,"TIDES & TYRES",true);
            House(new Vector3(-34,0,-111),9,10,7.5f,"BAYVIEW STORES",true);
            House(new Vector3(-17,0,-109),11,10,6.6f,"THE DAILY MILE",true);
            House(new Vector3(1,0,-111),9,9,7.4f,"POSTCARDS & FINDS",false);
            House(new Vector3(18,0,-117),10,10,7.8f,"COAST HOUSE",false);
            foreach(float x in new[]{-80f,-40f, -7f,12f}) {Tree(new Vector3(x,0,-101),.8f);Furniture(new Vector3(x+3,0,-101));}
            foreach(float x in new[]{-77f,-55f,-28f,-3f}) {
                // Keep the drivable forecourt clear. Posts belong at the rear
                // edge between frontages, not across the resident approach.
                Piece("Lantern post",new Vector3(x,2.2f,-103),new Vector3(.10f,4.4f,.10f),teal,true);
                Piece("Lantern frame",new Vector3(x,4.55f,-103),new Vector3(.52f,.55f,.52f),wood);
                Piece("Lantern glass",new Vector3(x,4.55f,-102.72f),new Vector3(.35f,.38f,.025f),cream);
                Piece("Lantern cap",new Vector3(x,4.9f,-103),new Vector3(.68f,.15f,.68f),teal);
            }
        }
        void Tree(Vector3 p,float scale)
        {
            // A soil opening conceals the paving beneath the trunk and gives
            // the spreading roots a grounded setting outside the driving lane.
            Piece("Olive soil bed",p+Vector3.up*.031f,new Vector3(1.68f,.03f,1.68f),wood);
            foreach(int side in new[]{-1,1}) {
                Piece("Tree bed limestone edging",p+new Vector3(0,.09f,side*.90f),new Vector3(1.98f,.18f,.18f),stone,true);
                Piece("Tree bed limestone edging",p+new Vector3(side*.90f,.09f,0),new Vector3(.18f,.18f,1.62f),stone,true);
            }
            planting.Plant(plantingRoot,p-Vector3.up*.055f,scale,1,Mathf.Abs(Mathf.RoundToInt(p.x))%3);
        }
        void Furniture(Vector3 p)
        {
            foreach(float x in new[]{-.6f,.6f})Piece("Bench legs",p+new Vector3(x,.35f,0),new Vector3(.12f,.7f,.6f),metal,true);
            for(int i=0;i<4;i++)Piece("Bench seat plank",p+new Vector3(0,.72f,-.3f+i*.18f),new Vector3(2,.1f,.14f),wood,true);
            for(int i=0;i<3;i++)Piece("Bench back plank",p+new Vector3(0,.95f+i*.17f,-.36f),new Vector3(2,.13f,.09f),wood,true);
        }
        public static readonly Vector3 PitStopSite=new(-66,0,-110);
        public static void Create()
        {
            var kit=new VillageKit();kit.Build();
        }
        void Build()
        {
            Piece("Garage and shop forecourt",new Vector3(-39,.007f,-97),new Vector3(78,.014f,13),stone);
            // Paving joints are colour/inset details, with the existing continuous
            // island collider beneath. Cars do not hit thousands of paving colliders.
            for(int x=-77;x<1;x+=3)for(int z=-103;z< -91;z+=3) {
                Piece("Forecourt paving seam",new Vector3(x,.017f,z),new Vector3(.025f,.006f,2.97f),wood);
                Piece("Forecourt paving seam",new Vector3(x+1.5f,.017f,z+1.5f),new Vector3(2.97f,.006f,.025f),wood);
            }
            // Bayview's Pit Stop (garage + parts; the tutorial town has no body shop) replaces the
            // old Harbour Garage at the west end of the strip, pylon on the open western side.
            // Outside `root`: the village is statically batched and the pylon tyre turns.
            if(BayviewKit.Has("pit_stop_2"))PitStop.Build("bayview",false,PitStopSite,0,null);else Garage(new Vector3(-65,0,-109));
            if(BayviewKit.Has("cafe"))Street();else LegacyStreet();
            if(BayviewKit.Has("sea_wall"))Harbour();
            if(BayviewKit.Has("kiosk"))GreenAndCarPark();
            var sign=Piece("Village welcome board",new Vector3(-88.5f,1.2f,-96),new Vector3(2.5f,2.4f,.20f),wood,true);
            Sign("BAYVIEW\nSmall cars\nBright roads",sign.transform.position+Vector3.forward*.12f,.048f,new Color(1,.94f,.74f));
            // All architectural pieces share a small material palette. Static
            // batching combines render work without removing editable modules.
            foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>())if(renderer.GetComponent<TextMesh>()==null)renderer.gameObject.isStatic=true;
            StaticBatchingUtility.Combine(root.gameObject);
        }
    }
}
