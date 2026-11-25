#if TESTS
using System;using System.IO;using System.Linq;using System.Drawing;using System.Collections.Generic;using System.Windows.Forms;
namespace PaperDays {public static class Version57Tests {
static IEnumerable<Control> All(Control c){foreach(Control x in c.Controls){yield return x;foreach(var y in All(x))yield return y;}}
public static int Run(string dir){Directory.CreateDirectory(dir);Data.Root=dir;OnlineData.Enabled=false;L.Language="zh-CN";int failed=0;var log=new List<string>();Action<bool,string> check=(ok,n)=>{log.Add((ok?"PASS ":"FAIL ")+n);if(!ok)failed++;};try{
foreach(var mark in new[]{"星z座z屋","星$座$屋","星 Z 座 Z 屋","星座屋"})check(CompleteHoroscopes.Clean("保留正文。 "+mark)=="保留正文。","watermark "+mark);
foreach(var lang in new[]{"zh-CN","zh-TW","en","ko","ja"}){L.Language=lang;check(InterpretationNotes.For("work").Contains("\n\n"),"multi-paragraph localized notes "+lang);}L.Language="zh-CN";
foreach(var skin in new[]{"glass","glass-light","rose"}){
var state=new State{Skin=skin,ShowCalendar=false,ShowWidget=false,ShowPlans=true};Skins.Apply(state);var scene=new Scene{State=state};state.WidgetLayouts[scene.PlacementKey]=new[]{new WidgetPlacement{Id="plans",X=.1f,Y=.03f,W=.4f,H=.9f}};
for(int i=0;i<3;i++)state.Todos.Add(new Todo{Title="已有待办 "+i});
using(var host=new Form{ClientSize=new Size(1000,800)})using(var view=new SceneView(scene){Dock=DockStyle.Fill,KeepInlineOnBlur=true}){
host.Controls.Add(view);host.Show();Application.DoEvents();view.Refresh();using(var render=scene.Render(view.ClientSize)){}check(scene.Hits.Any(h=>h.Action=="addtodo"),"desktop add uses inline action "+skin);
view.SaveInlineTodo=(id,text)=>{state.Todos.Add(new Todo{Title=text});Data.Write(state);return true;};int forms=Application.OpenForms.Count;
view.BeginInline();Application.DoEvents();view.Refresh();using(var render=scene.Render(view.ClientSize)){}var editor=scene.Hits.Single(h=>h.Action=="todoeditor");check(scene.ComponentRects["plans"].Contains(editor.Box),"editor inside plan component "+skin);var lastRow=scene.Hits.Last(h=>h.Action=="planedit").Box;
check(Math.Abs(editor.Box.Top-lastRow.Bottom)<1&&Math.Abs(editor.Box.Left-lastRow.Left)<1,"new editor follows last row and aligns "+skin);
check(scene.Hits.Count(h=>h.Action=="tododelete")==3&&scene.Hits.Any(h=>h.Action=="todocancel"),"row delete and draft cancel targets "+skin);
check(Application.OpenForms.Count==forms,"no task dialog opened "+skin);
view.InlineInput.Text="新增待办测试";view.FinishInline();Application.DoEvents();view.Refresh();using(var render=scene.Render(view.ClientSize)){}var todo=state.Todos.Last();check(Data.Read().Todos.Any(t=>t.Id==todo.Id),"todo persisted "+skin);check(scene.Hits.Any(h=>h.Id==todo.Id&&h.Action=="planedit"),"new todo visible after save "+skin);
view.BeginInline();view.InlineInput.Text="保留失败草稿";view.SaveInlineTodo=(id,text)=>false;check(!view.FinishInline()&&view.InlineInput.Text=="保留失败草稿","failed save retains draft "+skin);view.SaveInlineTodo=(id,text)=>true;view.FinishInline();
var delete=scene.Hits.First(h=>h.Action=="tododelete");state.DeleteTodo(delete.Id);Data.Write(state);check(!Data.Read().Todos.Any(t=>t.Id==delete.Id),"delete persists "+skin);
for(int i=0;i<25;i++)state.Todos.Add(new Todo{Title="长列表 "+i});view.BeginInline();view.Refresh();using(var render=scene.Render(view.ClientSize)){}
check(scene.Hits.Any(h=>h.Action=="todoeditor"&&scene.ComponentRects["plans"].Contains(h.Box)),"long list editor remains visible "+skin);
view.InlineInput.Text="";view.FinishInline();host.Close();}
}
using(var f=new DivinationResultDialog(new DivinationRecord{A=3,B=9,C=27,Category="工作"})){f.Show();Application.DoEvents();check(!All(f).OfType<Button>().Any(b=>b.Text=="关闭"),"no lower close button in divination result");check(All(f).OfType<Label>().Count(l=>l.Text==InterpretationNotes.Heading)==5,"five expanded category readings");f.Close();}
}catch(Exception e){failed++;log.Add("ERROR "+e);}File.WriteAllLines(Path.Combine(dir,"test-results.txt"),log);return failed==0?0:1;}
}}
#endif
