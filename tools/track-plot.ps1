# Draw a track-eval.py design (its --out JSON) as a PNG plan: the line, its start (green dot), its markers labelled,
# and a 100 m grid. The track-design skill's "look at the plan every round".
# Usage: .\tools\track-plot.ps1 review\tmp\mine.json [-Out review\tmp\mine.png] [-Size 900]
param([Parameter(Mandatory)][string]$Json,[string]$Out,[int]$Size=900)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$d=Get-Content $Json -Raw|ConvertFrom-Json;$pts=$d.pts;if(-not $Out){$Out=[IO.Path]::ChangeExtension($Json,'.png')}
$xs=$pts|ForEach-Object{$_[0]};$ys=$pts|ForEach-Object{$_[1]}
$x0=($xs|Measure-Object -Minimum).Minimum-60;$x1=($xs|Measure-Object -Maximum).Maximum+60;$y0=($ys|Measure-Object -Minimum).Minimum-60;$y1=($ys|Measure-Object -Maximum).Maximum+60
$scale=$Size/[Math]::Max($x1-$x0,$y1-$y0)
$bmp=New-Object System.Drawing.Bitmap $Size,$Size;$g=[System.Drawing.Graphics]::FromImage($bmp);$g.SmoothingMode='AntiAlias';$g.Clear([System.Drawing.Color]::White)
function P($x,$y){New-Object System.Drawing.PointF ([float](($x-$x0)*$scale)),([float]($Size-($y-$y0)*$scale))}
$grid=New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(40,0,0,0)),1
for($x=[Math]::Ceiling($x0/100)*100;$x -lt $x1;$x+=100){$g.DrawLine($grid,(P $x $y0),(P $x $y1))}
for($y=[Math]::Ceiling($y0/100)*100;$y -lt $y1;$y+=100){$g.DrawLine($grid,(P $x0 $y),(P $x1 $y))}
$line=New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255,40,60,90)),4
$arr=[System.Drawing.PointF[]]($pts|ForEach-Object{P $_[0] $_[1]});$g.DrawLines($line,$arr)
$s=P $pts[0][0] $pts[0][1];$g.FillEllipse([System.Drawing.Brushes]::LimeGreen,$s.X-8,$s.Y-8,16,16)
$font=New-Object System.Drawing.Font 'Arial',11,([System.Drawing.FontStyle]::Bold)
foreach($m in $d.marks){$q=P $m[1] $m[2];$g.FillEllipse([System.Drawing.Brushes]::OrangeRed,$q.X-5,$q.Y-5,10,10);$g.DrawString($m[0],$font,[System.Drawing.Brushes]::Black,$q.X+7,$q.Y-8)}
$r=$d.res;$g.DrawString("$($r.name)  lap $($r.lap_m) m  corners $($r.corners)  turn/km $($r.turning_deg_per_km)  curved $($r.curved_share)  L1 $($r.L1) L2 $($r.L2)  (100 m grid)",$font,[System.Drawing.Brushes]::DimGray,8,8)
$bmp.Save($Out,[System.Drawing.Imaging.ImageFormat]::Png);$g.Dispose();$bmp.Dispose();"plot: $Out"
