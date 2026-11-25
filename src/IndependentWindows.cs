using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PaperDays
{
    public static class IndependentWindows
    {
        static readonly Dictionary<string, Form> open = new Dictionary<string, Form>();
        /// <summary>
        /// 显示窗口或对话框，并返回用户操作结果。
        /// </summary>
        public static Task<DialogResult> Show(Form form)
        {
            string key = form.GetType().FullName + "|" + form.Text;
            Form previous;
            if (open.TryGetValue(key, out previous) && !previous.IsDisposed)
            {
                previous.WindowState = FormWindowState.Normal;
                previous.Activate();
                return Task.FromResult(DialogResult.Cancel);
            }

            var done = new TaskCompletionSource<DialogResult>();
            open[key] = form;
            form.FormClosed += delegate
            {
                open.Remove(key);
                done.TrySetResult(form.DialogResult);
            };
            try
            {
                WindowPlacement.Prepare(form);
                form.Show();
            }
            catch (Exception ex)
            {
                open.Remove(key);
                done.TrySetException(ex);
            }

            return done.Task;
        }

        /// <summary>
        /// 处理IndependentWindows中的 Prompt 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        public static async Task<string> Prompt(string title, string initial = "")
        {
            using (var f = new Form())
            {
                UI.Style(f, title, 440, 145);
                var t = UI.Input(f, initial, 24, 24, 390);
                t.MaxLength = 120;
                f.AcceptButton = UI.Button(f, L.T("保存"), 322, 82, 92, delegate
                {
                    if (!String.IsNullOrWhiteSpace(t.Text))
                    {
                        f.DialogResult = DialogResult.OK;
                        f.Close();
                    }
                });
                return await Show(f) == DialogResult.OK ? t.Text.Trim() : null;
            }
        }

        /// <summary>
        /// 关闭当前登记的独立窗口。
        /// </summary>
        public static void CloseAll()
        {
            foreach (var f in new List<Form>(open.Values))
                if (!f.IsDisposed)
                    f.Close();
        }
    }
}