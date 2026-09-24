namespace LittleRoad
{
    // Diagnostic cadence only. Start on movement, then retain stops/rejections too.
    public sealed class OpeningCaptureWindow
    {
        double first=double.NaN,next;
        public bool Capture(int id,double timestamp,float speed,bool finished)
        {
            if(id!=0||finished)return false;
            if(double.IsNaN(first)) {
                if(speed<2)return false;
                first=timestamp;next=timestamp;
            }
            if(timestamp>first+40||timestamp<next)return false;
            next=timestamp+.25;
            return true;
        }
    }
}
