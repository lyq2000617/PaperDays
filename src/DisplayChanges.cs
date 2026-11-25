using System;
using System.Windows.Forms;
namespace PaperDays {
 public sealed partial class Controller {
  Timer displayChangeTimer;
  void StartDisplayWatch() {
   displayChangeTimer=new Timer{Interval=250};
   displayChangeTimer.Tick+=delegate{displayChangeTimer.Stop();if(!stopping&&!hidden)Connect();};
   Microsoft.Win32.SystemEvents.DisplaySettingsChanged+=DisplayChanged;
  }
  void DisplayChanged(object sender,EventArgs args) {
   if(stopping||dispatcher.IsDisposed||!dispatcher.IsHandleCreated)return;
   try {dispatcher.BeginInvoke((Action)delegate{if(stopping)return;displayChangeTimer.Stop();displayChangeTimer.Start();});}catch(InvalidOperationException){}
  }
  void StopDisplayWatch() {
   Microsoft.Win32.SystemEvents.DisplaySettingsChanged-=DisplayChanged;
   if(displayChangeTimer!=null)displayChangeTimer.Dispose();
  }
 }
}
