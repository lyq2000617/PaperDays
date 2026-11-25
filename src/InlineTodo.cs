using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PaperDays
{
    public partial class SceneView
    {
        ReadableTodoInput inlineInput;
        Form inlineHost;
        internal bool IsolatedInput;
#if TESTS
        /// <summary>测试用：其他程序抢走前台时不结束输入，让渲染检查结果稳定。</summary>
        internal bool KeepInlineOnBlur;
        bool SaveOnBlur
        {
            /// <summary>读取失去焦点时是否保存。</summary>
            get
            {
                return !KeepInlineOnBlur;
            }
        }
#else
        const bool SaveOnBlur = true;
#endif
        bool finishing;
        string editingId;
        Hit pressedHit;
        public Func<string, string, bool> SaveInlineTodo;
        public Action<Hit> HitAction;
        internal ReadableTodoInput InlineInput
        {
            /// <summary>
            /// 读取InlineInput属性，供InlineTodo使用。
            /// </summary>
            get
            {
                return inlineInput;
            }
        }

        /// <summary>
        /// 启动行内待办编辑并准备可见的输入宿主。
        /// </summary>
        internal void BeginInline(string id = null)
        {
            if (!FinishInline())
                return;
            editingId = id;
            if (id == null)
                scene.State.TodoPage = 99999;
            var todo = scene.State.Todos.FirstOrDefault(t => t.Id == id);
            scene.InlineTodo = true;
            scene.ScrollOffsets["plansday"]=float.MaxValue;
            scene.InlineId = id;
            scene.InlineHeight = 45;
            inlineInput = new ReadableTodoInput
            {
                Multiline = true,
                AcceptsReturn = true,
                WordWrap = true,
                BorderStyle = BorderStyle.None,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                BackColor = Palette.Paper,
                ForeColor = ReadableTodoInput.TextInk,
                Font = UiFont.New(11),
                Text = todo == null ? "" : todo.Title,
                AccessibleName = "待办内容，Enter换行，点击其他位置保存"
            };
            if (this is DesktopSurface || IsolatedInput)
            {
                inlineHost = new TodoInputHost
                {
                    FormBorderStyle = FormBorderStyle.None,
                    ShowInTaskbar = false,
                    StartPosition = FormStartPosition.Manual,
                    BackColor = Palette.Paper,
                    ForeColor = ReadableTodoInput.TextInk,
                    AutoScaleMode = AutoScaleMode.None,
                    Text = "待办输入"
                };
                inlineHost.Controls.Add(inlineInput);
                inlineInput.Dock = DockStyle.Fill;
                inlineHost.Deactivate += delegate
                {
                    if (!finishing && SaveOnBlur && !IsDisposed && IsHandleCreated)
                        BeginInvoke((Action)delegate
                        {
                            if (inlineHost != null && !inlineHost.ContainsFocus)
                                FinishInline();
                        });
                };
            }
            else
                Controls.Add(inlineInput);
            inlineInput.TextChanged += delegate
            {
                SizeInline();
            };
            inlineInput.Leave += delegate
            {
                if (!finishing && SaveOnBlur)
                    FinishInline();
            };
            inlineInput.LostFocus += delegate
            {
                if (!finishing && SaveOnBlur)
                    FinishInline();
            };
            inlineInput.KeyDown += delegate (object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter && e.Control)
                {
                    e.SuppressKeyPress = true;
                    FinishInline();
                }
            };
            Invalidate();
            Update();
            SizeInline();
            if (inlineHost != null)
            {
                PlaceInlineEditor();
                inlineHost.Show();
                inlineHost.Activate();
            }

            inlineInput.Focus();
            inlineInput.SelectionStart = inlineInput.TextLength;
        }

        /// <summary>
        /// 处理InlineTodo中的 SizeInline 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        void SizeInline()
        {
            if (inlineInput == null)
                return;
            int width = Math.Max(80, inlineInput.Width - 20);
            var size = TextRenderer.MeasureText(inlineInput.Text + "\n", inlineInput.Font, new Size(width, 10000), TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);
            scene.InlineHeight = Math.Max(45, Math.Min(200, size.Height / Math.Max(.3f, scene.Layout == null ? 1 : scene.Layout.Scale) + 8));
            Invalidate();
        }

        /// <summary>
        /// 处理InlineTodo中的 PlaceInlineEditor 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        protected void PlaceInlineEditor()
        {
            if (inlineInput == null)
                return;
            var hit = scene.Hits.FirstOrDefault(h => h.Action == "todoeditor");
            if (hit != null)
            {
                var bounds = Rectangle.Round(hit.Box);
                if (inlineHost != null)
                {
                    var screenBounds = RectangleToScreen(bounds);
                    if (inlineHost.Bounds != screenBounds)
                        inlineHost.Bounds = screenBounds;
                }
                else if (inlineInput.Bounds != bounds)
                    inlineInput.Bounds = bounds;
                inlineInput.BringToFront();
            }
        }

        /// <summary>
        /// 提交行内输入；保存失败时保留编辑状态。
        /// </summary>
        internal bool FinishInline()
        {
            if (inlineInput == null || finishing)
                return true;
            finishing = true;
            var input = inlineInput;
            string text = input.Text.Replace("\r\n", "\n").Replace("\n", "\r\n").Trim();
            bool ok = true;
            try
            {
                if (text.Length > 0)
                {
                    if (SaveInlineTodo != null)
                        ok = SaveInlineTodo(editingId, text);
                    else
                    {
                        var todo = scene.State.Todos.FirstOrDefault(t => t.Id == editingId);
                        if (todo == null)
                            scene.State.Todos.Add(new Todo { Title = text });
                        else
                            todo.Title = text;
                    }
                }

                if (!ok)
                {
                    input.Focus();
                    return false;
                }

                if (editingId == null && text.Length > 0)
                    scene.State.TodoPage = 99999;
                inlineInput = null;
                scene.InlineTodo = false;
                scene.ScrollOffsets["plansday"]=float.MaxValue;
                scene.InlineId = null;
                if (inlineHost != null)
                {
                    var host = inlineHost;
                    inlineHost = null;
                    host.Controls.Remove(input);
                    host.Close();
                    host.Dispose();
                }
                else
                    Controls.Remove(input);
                input.Dispose();
                Invalidate();
                return true;
            }
            finally
            {
                finishing = false;
            }
        }

        /// <summary>
        /// 处理InlineTodo中的 HandleInlineClick 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        bool HandleInlineClick(Point point)
        {
            Hit hit = scene.At(point, ClientSize);
            pressedHit = null;
            if (hit == null)
            {
                FinishInline();
                return false;
            }

            if(hit.Action=="todocancel") { if(inlineInput!=null)inlineInput.Text=""; FinishInline(); return true; }
            if(hit.Action=="fortuneinput") { BeginFortuneInput(Int32.Parse(hit.Id)); return true; }
            if(hit.Action=="fortuneclear") { ClearFortuneInput(); return true; }
            if (hit.Action == "addtodo" || hit.Action == "planadd")
            {
                BeginInline();
                return true;
            }

            if (hit.Action == "todofull" && scene.PlanMode == "day")
            {
                BeginInline(hit.Id);
                return true;
            }

            if (!FinishInline())
                return true;
            if (HitAction != null)
            {
                HitAction(hit);
                return true;
            }

            return false;
        }
    }
}