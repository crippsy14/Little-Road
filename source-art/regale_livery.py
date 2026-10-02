"""The Regale's livery: Duke Rolo's racing colours in the manner of a modern Le Mans prototype, drawn as flat
artwork and projected onto the car.

Version 2 (Richard, 1 Oct, with photos of a current hydrogen prototype concept: "something of this ilk"): a
near-black body with a bright purple nose band, thin angular gold and cream linework, a big italic outlined number
in a slanted frame, a gold wing endplate and the crest on a badge at the top of the fin. Duke Rolo's own marks
throughout; nothing of the real car's sponsors or makers.

The texture (2048 px square) holds three views of the car at 0.20 UV per metre (about 2.4 mm a pixel):
  right side  u = .5 + y*S, v = .780 + (z - .08)*S   (nose to the right, reads front to back)
  left side   u = .5 - y*S, v = .555 + (z - .08)*S   (nose to the left, so its lettering reads the right way too)
  top         u = .5 + y*S, v = .270 - x*S           (seen from above, nose to the right)
plus a carbon patch for the floor. livery_uvs() gives each face of the LiveryMap material the view its normal
faces. The artwork is filled 2D curves and text (the game's own Lilita One and Nunito) in a scene of its own,
rendered flat with Workbench, then graded with WorldLook.Surface's numbers (saturation x.90 capped at .72, value
knee .55, roll-off .6, cap .82) so it sits with the world's colours.
"""
import math
import numpy as np
import bpy

S=.20;ZB=.08
NOIR=(.075,.055,.115);PURPLE=(.50,.30,.86);DEEP=(.20,.10,.38);GOLD=(.96,.74,.30);CREAM=(.97,.94,.86);INK=(.03,.025,.04)
LILAC=(.70,.58,.96);RED=(.86,.18,.14);YELLOW=(.98,.82,.12);WHITE=(.98,.98,.97);MINT=(.25,.80,.62)
BOTTOM=(.99,.008)

def right(y,z):return (.5+y*S,.780+(z-ZB)*S)
def left(y,z):return (.5-y*S,.555+(z-ZB)*S)
def top(y,x):return (.5+y*S,.270-x*S)

class Canvas:
    def __init__(self,fonts):
        self.scene=bpy.data.scenes.new('Livery');self.col=self.scene.collection;self.mats={};self.z=0.0
        self.display=bpy.data.fonts.load(str(fonts/'LilitaOne-Regular.ttf'));self.small=bpy.data.fonts.load(str(fonts/'Nunito-Black.ttf'))
    def mat(self,rgb):
        key=tuple(round(c,4) for c in rgb)
        if key not in self.mats:
            m=bpy.data.materials.new('Livery ink');m.diffuse_color=(*srgb_to_linear(rgb),1);self.mats[key]=m
        return self.mats[key]
    def layer(self):
        self.z+=.001;return self.z
    def shape(self,loops,rgb,name='Livery shape'):
        """Filled 2D shape: the first loop is the outline, any further loops are holes."""
        cu=bpy.data.curves.new(name,'CURVE');cu.dimensions='2D';cu.fill_mode='BOTH'
        for loop in loops:
            sp=cu.splines.new('POLY');sp.points.add(len(loop)-1)
            for p,(u,v) in zip(sp.points,loop):p.co=(u,v,0,1)
            sp.use_cyclic_u=True
        o=bpy.data.objects.new(name,cu);o.location.z=self.layer();cu.materials.append(self.mat(rgb));self.col.objects.link(o);return o
    def text(self,body,centre,height,rgb,width=None,small=False,rot=0.0,spacing=1.0,shear=0.0,outline=0.0,outline_rgb=None):
        """Text centred on `centre` (UV), capitals about `height` tall, squeezed to `width` if given; an optional
        outline (drawn first, the glyphs grown by `outline`)."""
        objs=[]
        for grow,colour in ([(outline,outline_rgb)] if outline else [])+[(0,rgb)]:
            cu=bpy.data.curves.new('Livery text','FONT');cu.body=body;cu.font=self.small if small else self.display
            cu.align_x='CENTER';cu.align_y='CENTER';cu.size=height*1.08;cu.space_character=spacing;cu.shear=shear;cu.offset=grow
            o=bpy.data.objects.new('Livery text',cu);self.col.objects.link(o);cu.materials.append(self.mat(colour))
            o.location=(centre[0],centre[1],self.layer());o.rotation_euler.z=rot;objs.append(o)
        if width:
            self.scene.view_layers[0].update();dims=objs[-1].dimensions
            run=dims.x if abs(math.sin(rot))<.5 else dims.y
            if run>width:
                for o in objs:o.scale.x=o.scale.y=width/run
        return objs[-1]
    def render(self,path,size=2048):
        sc=self.scene;sc.render.engine='BLENDER_WORKBENCH';sc.display.shading.light='FLAT';sc.display.shading.color_type='MATERIAL'
        sc.display.render_aa='16';sc.render.resolution_x=sc.render.resolution_y=size;sc.render.resolution_percentage=100
        sc.view_settings.view_transform='Standard';sc.render.image_settings.file_format='PNG';sc.render.image_settings.color_mode='RGB'
        if sc.world is None:sc.world=bpy.data.worlds.new('Livery world')
        sc.world.color=srgb_to_linear(INK)
        cd=bpy.data.cameras.new('Livery camera');cd.type='ORTHO';cd.ortho_scale=1;cd.clip_start=.001;cd.clip_end=100
        cam=bpy.data.objects.new('Livery camera',cd);self.col.objects.link(cam);cam.location=(.5,.5,10);sc.camera=cam
        sc.render.filepath=str(path);bpy.ops.render.render(write_still=True,scene=sc.name)

def srgb_to_linear(c):return tuple(((x+.055)/1.055)**2.4 if x>.04045 else x/12.92 for x in c)

# ---------------------------------------------------------------- geometry helpers (in car metres)
def circle(c,r,n=48):return [(c[0]+r*math.cos(a),c[1]+r*math.sin(a)) for a in np.linspace(0,math.tau,n,endpoint=False)]
def rect(y0,y1,z0,z1):return [(y0,z0),(y1,z0),(y1,z1),(y0,z1)]
def rrect(y0,y1,z0,z1,r,steps=5):
    pts=[]
    for cy,cz,a0 in [(y1-r,z1-r,0),(y0+r,z1-r,90),(y0+r,z0+r,180),(y1-r,z0+r,270)]:
        for k in range(steps+1):a=math.radians(a0+90*k/steps);pts.append((cy+r*math.cos(a),cz+r*math.sin(a)))
    return pts
def band(points,half):
    """A ribbon of half-width `half` along a polyline, mitred at its corners (so angular lines stay crisp)."""
    P=[np.array(p,float) for p in points];L=[];R=[]
    for i,p in enumerate(P):
        if i==0:t=P[1]-P[0];t/=np.linalg.norm(t);n=np.array([-t[1],t[0]]);m=half
        elif i==len(P)-1:t=P[-1]-P[-2];t/=np.linalg.norm(t);n=np.array([-t[1],t[0]]);m=half
        else:
            t0=P[i]-P[i-1];t0/=np.linalg.norm(t0);t1=P[i+1]-P[i];t1/=np.linalg.norm(t1)
            n0=np.array([-t0[1],t0[0]]);n1=np.array([-t1[1],t1[0]]);n=n0+n1;n/=np.linalg.norm(n);m=half/max(.3,float(n@n0))
        L.append(p+n*m);R.append(p-n*m)
    return [tuple(p) for p in L]+[tuple(p) for p in R[::-1]]
def crown(c,w,rot=0.0):
    """A five-point crown, width w, centred at c, its top towards rot (0 = +v)."""
    h=w*.62;pts=[(-.5,0),(-.5,.55),(-.30,.28),(-.15,.72),(0,.40),(.15,.72),(.30,.28),(.5,.55),(.5,0)]
    ca,sa=math.cos(rot),math.sin(rot)
    return [(c[0]+(x*w)*ca-(y*h-.3*h)*sa,c[1]+(x*w)*sa+(y*h-.3*h)*ca) for x,y in pts]
def castle(c,w,rot=0.0):
    """The castle on the crest: a gatehouse between two towers, crenellated."""
    h=w*.7;u=[(-.5,-.5),(.5,-.5),(.5,.32),(.42,.32),(.42,.42),(.34,.42),(.34,.32),(.26,.32),(.26,.08),(.12,.08),(.12,.2),(.04,.2),(.04,.08),
             (-.04,.08),(-.04,.2),(-.12,.2),(-.12,.08),(-.26,.08),(-.26,.32),(-.34,.32),(-.34,.42),(-.42,.42),(-.42,.32),(-.5,.32)]
    ca,sa=math.cos(rot),math.sin(rot)
    return [(c[0]+(x*w)*ca-(y*h)*sa,c[1]+(x*w)*sa+(y*h)*ca) for x,y in u]
def rotate_loop(loop,rot,at):
    ca,sa=math.cos(rot),math.sin(rot)
    return [(at[0]+x*ca-y*sa,at[1]+x*sa+y*ca) for x,y in loop]

# ---------------------------------------------------------------- the design
def side_art(cv,m):
    """One side, through mapper m (car y, z -> UV). Text is placed at mapped points, never mirrored."""
    def P(loop):return [m(y,z) for y,z in loop]
    def line(pts,w,rgb):cv.shape([P(band(pts,w/2))],rgb)
    cv.shape([P(rect(-2.7,2.7,0,1.25))],NOIR)
    # The purple nose band, its trailing edge raked back ahead of the front wheel, edged in gold.
    cv.shape([P([(2.7,0),(2.7,1.25),(2.12,1.25),(1.96,.62),(2.16,0)])],PURPLE)
    line([(2.12,1.25),(1.96,.62),(2.16,0)],.016,GOLD)
    # The cavity behind the front wheel and the sidepod mouth in it: black.
    cv.shape([P(rect(.46,1.34,0,.555))],INK)
    # Angular linework (as the reference's orange and white): a gold line along the door and over the rear wheel,
    # stepping down to the sill between; a cream line shadowing it.
    line([(1.36,.665),(-.06,.665),(-.30,.42),(-1.10,.42),(-1.32,.80),(-2.12,.80),(-2.36,.56)],.020,GOLD)
    line([(1.36,.625),(-.02,.625),(-.26,.38),(-1.08,.38)],.008,CREAM)
    line([(-1.28,.72),(-2.06,.72),(-2.24,.56)],.008,CREAM)
    # The number: a big italic 01, purple outlined in cream, in a slanted frame (as the reference's lettering box).
    frame=[(-1.06,.20),(-.20,.20),(-.10,.36),(-.96,.36)]
    line(frame+[frame[0]],.010,CREAM)
    cv.text('01',m(-.58,.282),.24*S,PURPLE,width=.62*S,shear=.25,outline=.010*S*2.5,outline_rgb=CREAM)
    # The driver's name on the door strip over the cavity, with the crown.
    cv.shape([P(crown((.40,.598),.05))],GOLD)
    cv.text('DUKE ROLO',m(.70,.595),.032*S,CREAM,width=.40*S)
    # Small sponsors: contingency stickers on the sill, the railway on the tail flank, the gelato in the nose band.
    for k,(label,bg,fg) in enumerate([('OLIVE GROVES',MINT,WHITE),("PIP'S POSTCARDS",YELLOW,INK),('VALLEY RACEWAY',WHITE,RED),('CASTLE TOURS',DEEP,GOLD)]):
        y0=-1.10+k*.235;cv.shape([P(rrect(y0,y0+.215,.115,.165,.01))],bg);cv.text(label,m(y0+.1075,.14),.026*S,fg,width=.19*S,small=True)
    cv.text("BERT'S RAILWAY",m(-.62,.53),.026*S,CREAM,width=.30*S,small=True)
    # The gearbox block in the open rear: carbon black.
    cv.shape([P(rect(-2.40,-2.02,0,.56))],INK)
    cv.text('GINA GELATO',m(2.26,.34),.036*S,WHITE,width=.36*S)
    cv.text('SCUDERIA ROLO',m(2.26,.285),.020*S,DEEP,width=.36*S,small=True)
    # Fin: the car's name in gold, the crest badge at its top rear (where the reference carries its maker's badge).
    cv.text('REGALE',m(-.95,1.0),.050*S,GOLD,width=.55*S)
    cv.shape([P(rrect(-2.00,-1.72,.945,1.075,.012))],CREAM)
    cv.shape([P(crown((-1.86,1.046),.11))],GOLD);cv.shape([P(castle((-1.86,.98),.12))],NOIR)
    # Endplate: gold, the team's name.
    cv.shape([P([(-2.52,.64),(-2.02,.64),(-1.92,.78),(-1.96,1.13),(-2.52,1.15)])],GOLD)
    cv.text('SCUDERIA',m(-2.24,.98),.036*S,NOIR,width=.40*S);cv.text('ROLO',m(-2.24,.83),.10*S,NOIR,width=.42*S)
    # Safety markings: the rescue arrow at the door, the high-voltage triangle at the battery.
    cv.shape([P([(1.10,.565),(1.22,.595),(1.10,.625)])],RED);cv.text('RESCUE',m(1.00,.595),.016*S,WHITE,width=.13*S,small=True)
    tri=[(-1.76,.17),(-1.64,.17),(-1.70,.27)];cv.shape([P(tri)],YELLOW)
    cv.shape([P([(-1.705,.255),(-1.71,.215),(-1.697,.215),(-1.70,.185),(-1.688,.23),(-1.70,.23)])],INK)

def top_art(cv):
    m=top
    def P(loop):return [m(y,x) for y,x in loop]   # loops given as (y, x)
    def line(pts,w,rgb):cv.shape([P(band(pts,w/2))],rgb)
    cv.shape([P(rect(-2.7,2.7,-1.4,1.4))],NOIR)
    # The purple nose band across the deck, a chevron at its back, edged in gold; the wordmark reads from the front.
    cv.shape([P([(2.7,-1.4),(2.7,1.4),(1.62,1.4),(1.30,0),(1.62,-1.4)])],PURPLE)
    line([(1.62,1.4),(1.30,0),(1.62,-1.4)],.016,GOLD)
    front=math.pi/2   # upright for someone standing in front of the car
    cv.text('REGALE',top(2.05,0),.12*S,CREAM,width=.95*S,rot=front)
    cv.text('SCUDERIA  ROLO',top(1.88,0),.034*S,DEEP,width=.55*S,rot=front,small=True)
    # Gold lines converging on the nose from the fender tops, gold along the rear fenders, a cream pair on the spine.
    for sx in (-1,1):
        line([(1.20,sx*.80),(1.80,sx*.60),(2.40,sx*.22)],.018,GOLD)
        line([(-1.30,sx*.83),(-2.36,sx*.83)],.016,GOLD)
        line([(.10,sx*.22),(-2.30,sx*.10)],.008,CREAM)
    # The crest on the engine cover and the number, readable from the chase camera (their tops towards the nose).
    up=-math.pi/2
    cv.shape([rotate_loop(crown((0,0),.16*S),up,top(-1.18,0))],GOLD)
    cv.shape([rotate_loop(castle((0,0),.16*S),up,top(-1.38,0))],GOLD)
    cv.text('1',top(-1.66,0),.11*S,CREAM,rot=up,shear=.25)
    for sx in (-1,1):cv.text('FUEL',top(-1.32,sx*.80),.022*S,GOLD,small=True,rot=up)
    cv.shape([[(.975,0),(1,0),(1,.016),(.975,.016)]],INK)

def grade(path):
    """WorldLook.Surface on every pixel (gamma-space HSV), so the artwork obeys the same colour limits."""
    img=bpy.data.images.load(str(path));w,h=img.size;px=np.array(img.pixels[:],np.float32).reshape(h,w,4)
    rgb=px[...,:3];mx=rgb.max(-1);mn=rgb.min(-1);v=mx;s=np.where(mx>1e-6,(mx-mn)/np.maximum(mx,1e-6),0)
    s2=np.minimum(s*.90,.72);v2=np.where(v>.55,.55+(v-.55)*.6,v);v2=np.clip(v2,.05,.82)
    with np.errstate(invalid='ignore',divide='ignore'):
        base=np.where(mx[...,None]>1e-6,rgb/np.maximum(mx,1e-6)[...,None],1)
        t=np.where(s[...,None]>1e-6,(1-base)/np.maximum(s,1e-6)[...,None],0)
    out=v2[...,None]*(1-t*s2[...,None]);px[...,:3]=np.clip(out,0,1)
    img.pixels[:]=px.ravel();img.filepath_raw=str(path);img.file_format='PNG';img.save();bpy.data.images.remove(img)

def paint(path,fonts):
    cv=Canvas(fonts)
    side_art(cv,right);side_art(cv,left);top_art(cv)
    cv.render(path);grade(path)
    for o in list(cv.scene.collection.objects):bpy.data.objects.remove(o,do_unlink=True)
    bpy.data.scenes.remove(cv.scene)

def livery_uvs(obj,material_name='LiveryMap'):
    """Each LiveryMap face takes the view its normal faces: sides when |n.x| > .35, the floor a carbon patch, else
    the top (front and rear faces too: the top view's ends run down them). The canopy and spine always take the top
    view (except thin upright plates like the fin), and so do walls facing in towards the centreline."""
    me=obj.data;uv=me.uv_layers.get('UVMap') or me.uv_layers.new(name='UVMap')
    slots=[i for i,m in enumerate(me.materials) if m and m.name.startswith(material_name)]
    n=len(me.polygons);normals=np.zeros(n*3);me.polygons.foreach_get('normal',normals);normals=normals.reshape(n,3)
    mats=np.zeros(n,np.int32);me.polygons.foreach_get('material_index',mats)
    totals=np.zeros(n,np.int32);me.polygons.foreach_get('loop_total',totals)
    nl=len(me.loops);lv=np.zeros(nl,np.int32);me.loops.foreach_get('vertex_index',lv)
    co=np.zeros(len(me.vertices)*3);me.vertices.foreach_get('co',co);co=co.reshape(-1,3)
    face_of=np.repeat(np.arange(n),totals);fn=normals[face_of];p=co[lv];out=np.zeros((nl,2))
    live=np.isin(mats[face_of],slots)
    side=np.abs(fn[:,0])>.35;under=(fn[:,2]<-.6)&(p[:,2]<.2)
    side&=~((np.abs(p[:,0])<.28)&(np.abs(fn[:,0])<.9))
    side&=(fn[:,0]*p[:,0]>0)|(np.abs(p[:,0])<.05)
    r=live&side&(fn[:,0]>0);l=live&side&(fn[:,0]<=0);t=live&~side&~under;b=~(r|l|t)
    out[r]=np.stack([.5+p[r,1]*S,.780+(p[r,2]-ZB)*S],-1)
    out[l]=np.stack([.5-p[l,1]*S,.555+(p[l,2]-ZB)*S],-1)
    out[t]=np.stack([.5+p[t,1]*S,.270-p[t,0]*S],-1)
    out[b]=BOTTOM;out=np.clip(out,0,1)
    uv.data.foreach_set('uv',out.ravel())
    for other in [x for x in me.uv_layers if x.name!=uv.name]:me.uv_layers.remove(other)
    uv=me.uv_layers[0];uv.active=True;uv.active_render=True
    print('LIVERY_UVS',dict(right=int(r.sum()),left=int(l.sum()),top=int(t.sum()),other=int(b.sum())))
