using System;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
namespace PaperDays {
 public class HexColorDialog : Form {
  public Color Color = Color.White;
  TextBox code; Label error; Panel swatch;
  /// <summary>创建只输入十六进制色号的颜色窗口，预览有效颜色并阻止无效输入提交。</summary>
  public HexColorDialog() {
   Text=L.T("自选颜色");ClientSize=new Size(340,174);Font=UiFont.New(10);BackColor=Palette.Paper;ForeColor=Palette.Ink;
   UI.Label(this,"颜色编码（#RRGGBB）",20,16,280);
   code=new TextBox{Bounds=new Rectangle(24,49,236,25),MaxLength=7};Controls.Add(code);
   swatch=new Panel{Bounds=new Rectangle(278,44,36,36),BackColor=Color};Controls.Add(swatch);
   error=new Label{Bounds=new Rectangle(20,88,300,24),ForeColor=Palette.Muted};Controls.Add(error);
   var cancel=UI.Button(this,L.T("取消"),110,123,94,delegate{DialogResult=DialogResult.Cancel;Close();});
   var ok=UI.Button(this,"确定",220,123,94,delegate {Color parsed;if(!TryParse(code.Text,out parsed)){error.Text="请输入 6 位十六进制色号";return;}Color=parsed;DialogResult=DialogResult.OK;Close();});
   AcceptButton=ok;CancelButton=cancel;
   code.TextChanged+=delegate{Color parsed;if(TryParse(code.Text,out parsed)){swatch.BackColor=parsed;error.Text="";}};
   Load+=delegate{code.Text=ColorTranslator.ToHtml(Color);if(!code.Text.StartsWith("#"))code.Text="#"+Color.R.ToString("X2")+Color.G.ToString("X2")+Color.B.ToString("X2");Chrome.Install(this);};
  }
  /// <summary>校验六位 RGB 色号；允许省略井号，不接受色名、透明度或其他格式。</summary>
  public static bool TryParse(string text,out Color color){color=Color.Empty;string value=(text??"").Trim().TrimStart('#');if(!Regex.IsMatch(value,"^[0-9a-fA-F]{6}$"))return false;color=ColorTranslator.FromHtml("#"+value);return true;}
 }
}
