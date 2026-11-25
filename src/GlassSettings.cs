using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
namespace PaperDays {
 public class GlassPreference { public int Outer=65,Content=25; }
 public static class GlassOptions {
  public static GlassPreference Get(State state,string skin,string component) {
   if(state.GlassPreferences==null)state.GlassPreferences=new Dictionary<string,GlassPreference>();
   string key=skin+":"+component;GlassPreference value;
   if(!state.GlassPreferences.TryGetValue(key,out value)||value==null){value=new GlassPreference{Content=component=="calendar"?30:25};state.GlassPreferences[key]=value;}
   return value;
  }
 }
 public partial class Scene {
  static void ContentGlass(Graphics g,RectangleF area) {if(Skins.Glass)GlassWindow.Card(g,area);}
 }
 public partial class SceneView {
  static Cursor LayoutCursor(Hit hit) {
   if(hit==null)return Cursors.Default;
   if(hit.Action=="layoutmove")return Cursors.SizeAll;
   if(hit.Action!="layoutresize")return Cursors.Default;
   string edge=hit.Id.Split(':')[1];
   return edge=="n"||edge=="s"?Cursors.SizeNS:edge=="e"||edge=="w"?Cursors.SizeWE:edge=="nw"||edge=="se"?Cursors.SizeNWSE:Cursors.SizeNESW;
  }
 }
 public sealed partial class Controller {
  bool isolatedWindows;
  Form componentManager,glassSettings;
  void OpenComponentManager() {
   if(stopping)return;
   if(componentManager!=null&&!componentManager.IsDisposed){componentManager.WindowState=FormWindowState.Normal;componentManager.Show();componentManager.Activate();return;}
   var form=new CompactDialog(L.T("桌面组件"),420,270);form.Icon=WindowIcons.Components;componentManager=form;
   var panel=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,Padding=new Padding(18)};form.Body.Controls.Add(panel);
   panel.Controls.Add(new Label{Text=L.T("隐藏不会删除内容，重新显示会保留位置。"),AutoSize=true,MaximumSize=new Size(360,0),Margin=new Padding(0,0,0,12)});
   var checks=new List<CheckBox>();bool syncing=false;
   foreach(var pair in new[]{new[]{"calendar","日历"},new[]{"widget","星座 / 占卜"},new[]{"plans","每日计划"}}){
    string id=pair[0];var box=new CheckBox{Text=L.T(pair[1]),AutoSize=true,Checked=scene.ComponentVisible(id),Tag=id,Margin=new Padding(0,6,0,6)};
    box.CheckedChanged+=delegate{if(!syncing)SetComponentVisible(id,box.Checked);};checks.Add(box);panel.Controls.Add(box);
   }
   Action sync=()=>{syncing=true;foreach(var box in checks)box.Checked=scene.ComponentVisible((string)box.Tag);syncing=false;};
   form.AddButton(L.T("全部显示"),delegate{ShowAllComponents();sync();},true);
   form.AddButton(L.T("恢复默认布局"),delegate{ResetComponentLayout();sync();});
   form.Activated+=delegate{sync();};form.FormClosed+=delegate{componentManager=null;};form.ApplyTheme();if(!isolatedWindows)form.Show();
  }
  void ShowAllComponents(){if(!state.ShowWidget&&state.Widget=="占卜")scene.ClearFortune();if(!Change(delegate{state.ShowCalendar=state.ShowWidget=state.ShowPlans=true;}))return;hidden=false;if(surfaces.Count==0)Connect();else RefreshSurface();}
  void ResetComponentLayout(){Change(delegate{state.WidgetLayouts.Remove(scene.PlacementKey);});scene.DragPreview=null;RefreshSurface();}
  void OpenGlassSettings() {
   if(glassSettings!=null&&!glassSettings.IsDisposed){glassSettings.Show();glassSettings.Activate();return;}
   var form=new CompactDialog(L.T("外观与皮肤"),600,650);glassSettings=form;
   var panel=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,Padding=new Padding(20)};form.Body.Controls.Add(panel);
   Action<string> heading=text=>panel.Controls.Add(new Label{Text=L.T(text),AutoSize=true,Font=UiFont.New(11,FontStyle.Bold),Margin=new Padding(0,10,0,8)});
   var tiles=new FlowLayoutPanel{AutoSize=true,WrapContents=true,Width=540,MaximumSize=new Size(540,0),Margin=new Padding(0)};
   heading("皮肤选择");panel.Controls.Add(tiles);
   var tileList=new List<SkinTile>();Action load=null;Action sync=null;
   var glass=new FlowLayoutPanel{AutoSize=true,FlowDirection=FlowDirection.TopDown,WrapContents=false,Margin=new Padding(0),Width=530};
   foreach(var theme in Skins.All.OrderByDescending(t=>Skins.IsGlass(t.Id))) {
    var selected=theme;var tile=new SkinTile{Theme=theme,Chosen=state.Skin==theme.Id,Size=new Size(166,126),Margin=new Padding(0,0,8,8),AccessibleName=L.T(theme.Name)};
    tile.Click+=delegate{if(!Change(delegate{if(!Skins.IsGlass(state.Skin))state.GlassBaseSkin=state.Skin;state.Skin=selected.Id;bool modeSaved;if(Skins.IsGlass(state.Skin))state.FillScreen=state.SkinDisplayModes!=null&&state.SkinDisplayModes.TryGetValue(state.Skin,out modeSaved)?modeSaved:false;Skins.Apply(state);}))return;foreach(var item in tileList){item.Chosen=item.Theme.Id==state.Skin;item.Invalidate();}glass.Visible=Skins.Glass;load();sync();if(Skins.Glass){panel.ScrollControlIntoView(glass);}RefreshSurface();};tiles.Controls.Add(tile);tileList.Add(tile);
   }
   var modeTitle=new Label{Text=L.T("显示方式"),AutoSize=true,Font=UiFont.New(11,FontStyle.Bold),Margin=new Padding(0,10,0,8)};panel.Controls.Add(modeTitle);var modes=new FlowLayoutPanel{AutoSize=true,Margin=new Padding(0)};panel.Controls.Add(modes);
   var widgets=new SoftButton{Name="mode-widgets",Text=L.T("桌面组件"),Size=new Size(220,36)};var fill=new SoftButton{Name="mode-fill",Text=L.T("铺满屏幕"),Size=new Size(220,36)};modes.Controls.Add(widgets);modes.Controls.Add(fill);panel.Controls.SetChildIndex(modeTitle,0);panel.Controls.SetChildIndex(modes,1);
   Action<bool> mode=value=>{if(!Change(delegate{state.FillScreen=value;if(state.SkinDisplayModes==null)state.SkinDisplayModes=new Dictionary<string,bool>();state.SkinDisplayModes[state.Skin]=value;}))return;scene.DragPreview=null;sync();RefreshSurface();};
   sync=()=>{widgets.Tag=state.FillScreen?null:"primary";fill.Tag=state.FillScreen?"primary":null;form.ApplyTheme();};widgets.Click+=delegate{mode(false);};fill.Click+=delegate{mode(true);};
   panel.Controls.Add(glass);glass.Controls.Add(new Label{Text=L.T("玻璃效果"),AutoSize=true,Font=UiFont.New(11,FontStyle.Bold),Margin=new Padding(0,14,0,8)});
   var tabs=new FlowLayoutPanel{AutoSize=true,Margin=new Padding(0,0,0,10)};glass.Controls.Add(tabs);string[] ids={"calendar","plans","widget"},names={"日历","每日计划","星座 / 占卜"};int component=0;bool loading=false;
   var tabButtons=new List<Button>();for(int i=0;i<3;i++){int index=i;var button=new SoftButton{Name="tab-"+ids[i],Text=L.T(names[i]),Size=new Size(166,36)};button.Click+=delegate{component=index;load();};tabButtons.Add(button);tabs.Controls.Add(button);}
   var outerLabel=new Label{AutoSize=true};var contentLabel=new Label{AutoSize=true};
   var outer=new GlassSlider{Width=510,AccessibleName=L.T("外围透明度")};var content=new GlassSlider{Width=510,AccessibleName=L.T("内容透明度")};
   glass.Controls.Add(outerLabel);glass.Controls.Add(outer);glass.Controls.Add(contentLabel);glass.Controls.Add(content);
   var applyAll=new CheckBox{Text=L.T("应用到全部组件"),AutoSize=true};var relief=new CheckBox{Text=L.T("日期起伏"),AutoSize=true,Checked=state.GlassRelief};
   var presets=new FlowLayoutPanel{AutoSize=true,Margin=new Padding(0,6,0,10)};glass.Controls.Add(presets);
   var options=new FlowLayoutPanel{AutoSize=true};options.Controls.Add(applyAll);options.Controls.Add(relief);glass.Controls.Add(options);
   Action labels=()=>{outerLabel.Text=L.T("外围透明度")+"   "+outer.Value+"%";contentLabel.Text=L.T("内容透明度")+"   "+content.Value+"%";};
   load=()=>{loading=true;var value=GlassOptions.Get(state,state.Skin,ids[component]);outer.Value=value.Outer;content.Value=value.Content;relief.Enabled=component==0;for(int i=0;i<3;i++)tabButtons[i].Tag=i==component?"primary":null;labels();form.ApplyTheme();loading=false;};
   Action preview=()=>{if(loading)return;foreach(string id in applyAll.Checked?ids:new[]{ids[component]}){var value=GlassOptions.Get(state,state.Skin,id);value.Outer=outer.Value;value.Content=content.Value;}labels();RequestRender();};
   outer.ValueChanged+=delegate{preview();};content.ValueChanged+=delegate{preview();};
   Action save=()=>{Change(delegate{});};outer.MouseUp+=delegate{save();};content.MouseUp+=delegate{save();};outer.KeyUp+=delegate{save();};content.KeyUp+=delegate{save();};
   foreach(var preset in new[]{new[]{"通透","80","45"},new[]{"均衡","65","30"},new[]{"清晰","45","15"}}){var choice=preset;var button=new SoftButton{Name="preset-"+choice[0],Text=L.T(choice[0]),Size=new Size(166,36)};button.Click+=delegate{outer.Value=int.Parse(choice[1]);content.Value=int.Parse(choice[2]);preview();save();};presets.Controls.Add(button);}
   applyAll.CheckedChanged+=delegate{if(applyAll.Checked){preview();save();}};
   relief.CheckedChanged+=delegate{if(!loading){Change(delegate{state.GlassRelief=relief.Checked;});RefreshSurface();}};
   form.AddButton(L.T("完成"),()=>form.Close(),true);form.AddButton(L.T("恢复默认"),()=>{foreach(string id in applyAll.Checked?ids:new[]{ids[component]})state.GlassPreferences.Remove(state.Skin+":"+id);load();save();RefreshSurface();});
   form.ScrollActionsWith(panel);form.Shown+=delegate{form.ActiveControl=null;panel.AutoScrollPosition=Point.Empty;};
   Action resize=()=>{
    int width=Math.Max(180,panel.ClientSize.Width-panel.Padding.Horizontal-SystemInformation.VerticalScrollBarWidth-8);
    tiles.MaximumSize=new Size(width,0);glass.MaximumSize=new Size(width,0);
    foreach(var tile in tileList)tile.Size=new Size(width<400?width-8:Math.Max(140,(width-16)/2),126);
    modes.MaximumSize=tabs.MaximumSize=presets.MaximumSize=options.MaximumSize=new Size(width,0);
    widgets.Width=fill.Width=Math.Max(130,(width-14)/2);
    foreach(var button in tabButtons)button.Width=Math.Max(125,(width-20)/3);
    foreach(Control button in presets.Controls)button.Width=Math.Max(90,(width-20)/3);
    outer.Width=content.Width=width-6;
   };panel.SizeChanged+=delegate{resize();};resize();
   form.FormClosed+=delegate{save();glassSettings=null;};glass.Visible=Skins.Glass;load();sync();if(!isolatedWindows)form.Show();
  }
  void ShowOrganizedTools() {
   var menu=new ThemedMenu();
   Func<string,ToolStripMenuItem> group=label=>{var entry=new ToolStripMenuItem(L.T(label));menu.Items.Add(entry);return entry;};
   Action<ToolStripMenuItem,string,Action> add=(parent,label,run)=>parent.DropDownItems.Add(L.T(label),null,delegate{dispatcher.BeginInvoke(run);});
   menu.Items.Add(L.T("外观与皮肤"),null,delegate{dispatcher.BeginInvoke((Action)ChooseSkin);});
   menu.Items.Add(L.T("显示屏幕"),null,delegate{dispatcher.BeginInvoke((Action)ChooseScreen);});
   AddLanguageMenu(menu);
   var components=group("桌面组件");add(components,"显示 / 隐藏",OpenComponentManager);add(components,"全部显示",ShowAllComponents);
   var layout=group("布局");add(layout,"恢复默认布局",ResetComponentLayout);add(layout,"找回屏幕外的组件",RecoverOffscreen);add(layout,"桌面图标留白",ChooseIconArea);
   var data=group("日历与数据");add(data,"日历管理",Settings);add(data,"课表管理",ImportCourses);add(data,"导出备份",Export);add(data,"恢复备份",Restore);
   var help=group("帮助与诊断");add(help,"新手引导",OpenGuide);add(help,"使用说明",()=>Notice.Show(L.T("拖动标题空白处移动；拖动边缘或四角缩放。关闭只隐藏组件，托盘右键的桌面组件可以恢复。")));
   add(help,"版本",()=>Notice.Show("PaperDays "+Program.Version));add(help,"查看诊断信息",()=>Notice.Show(DiagnosticReport(),L.T("查看诊断信息")));
   menu.Closed+=delegate{dispatcher.BeginInvoke((Action)(()=>menu.Dispose()));};menu.Show(Cursor.Position);
  }
 }
}


