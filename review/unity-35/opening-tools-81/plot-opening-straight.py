"""Visualise recorded opening-straight evidence, including spatial field distribution."""
import json,os,sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent/'python-packages'))
root=Path(sys.argv[1]);os.environ.setdefault('MPLCONFIGDIR',str(root/'.matplotlib-cache'))
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
from matplotlib.lines import Line2D
read=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
data=read(root/'opening-straight-analysis.json');gate=data['brakingZoneRouteMetres']
fig,axes=plt.subplots(3,2,figsize=(14,12),layout='constrained')
palette={'street':'#9da9b2','club':'#008a99','preview':'#e46d24'}
for col,mode in enumerate(['off','on']):
    d=data['results'][mode];t=read(root/mode/'race.track.json');equipment={c['id']:c['equipment'] for c in d['perCar']}
    for i in range(24):
        rows=[s for s in t['samples'] if s['id']==i and 0<=s['time']<=d['lastCarAtBrakingZone']+.1 and s['progress']<=gate+3]
        color=palette['preview' if i==0 else equipment[i]]
        axes[0,col].plot([s['time'] for s in rows],[s['progress'] for s in rows],color=color,lw=2.2 if i==0 else 1,alpha=1 if i==0 else .7,zorder=4 if i==0 else 2)
    axes[0,col].axhline(gate,color='#333',ls='--',lw=.8)
    axes[0,col].set(xlim=(0,19.5),ylim=(-55,295),xlabel='Seconds after green',ylabel='Progress from start line (m)',title='Reference' if mode=='off' else 'Dynamic paths')
    ax=axes[1,col];frame=d['fieldAtFirstArrival']
    ax.axhspan(-8,8,color='#eef1f3');ax.axhline(-8,color='#9da9b2');ax.axhline(8,color='#9da9b2')
    for c in frame['cars']:
        color=palette['preview' if c['id']==0 else equipment[c['id']]]
        ax.scatter(c['progress'],c['roadOffset'],color=color,s=55 if c['id']==0 else 27,zorder=3)
        if equipment[c['id']]=='club':ax.annotate(str(c['id']),(c['progress'],c['roadOffset']),xytext=(0,7),textcoords='offset points',ha='center',fontsize=8,color=color)
    ax.set(xlim=(140,285),ylim=(-9.5,10.5),xlabel='Progress (m); dots are car centres',ylabel='Lateral offset (m)',title=f"When leader reaches 280 m: {frame['time']:.2f} s | field spans {frame['spreadMetres']:.1f} m")
    ax=axes[2,0];frames=d['frames'];ax.plot([s['time'] for s in frames],[s['spreadMetres'] for s in frames],marker='o',ms=3,label='Reference' if mode=='off' else 'Dynamic',color='#415b76' if mode=='off' else '#008a99')
    rows=[s for s in t['samples'] if s['id']==0 and s['time']>=0 and s['progress']<=gate]
    axes[2,1].plot([s['time'] for s in rows],[s['speed']*3.6 for s in rows],label='Reference' if mode=='off' else 'Dynamic',color='#415b76' if mode=='off' else '#008a99')
axes[2,0].set(xlabel='Seconds after green (ends at leader arrival)',ylabel='First-to-last progress spread (m)',title='Compactness alone is not a pace score')
axes[2,1].set(xlabel='Seconds after green (ends at own 280 m arrival)',ylabel='Preview speed (km/h)',title='Preview car 0: acceleration on opening straight')
for ax in axes.flat:ax.grid(alpha=.15)
axes[2,0].legend(frameon=False);axes[2,1].legend(frameon=False)
fig.legend(handles=[Line2D([0],[0],color=palette[k],lw=2,label=label) for k,label in [('street','Street equipment'),('club','Club equipment'),('preview','Preview car 0 (club)')]],loc='outside lower center',ncol=3,frameon=False)
fig.suptitle('Cedar opening straight: same 24 cars, equipment and grid\nRecorded 10 Hz motion; snapshots interpolated at the same leader location, not the same time')
fig.savefig(root/'opening-straight.png',dpi=150)
print(root/'opening-straight.png')
