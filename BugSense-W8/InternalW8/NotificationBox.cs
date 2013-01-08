using BugSense_W8;
using System;
using System.Collections.Generic;
using Windows.UI.Popups;

namespace BugSense.InternalW8
{
    internal class NotificationBoxCommand
    {
        private string label;
        private Action act0;
        private Action<Object> act1;
        private Action<Object, Object> act2;
        private Action<Object, Object, Object> act3;
        private Action<Object, Object, Object, Object> act4;
        private Object[] args = new Object[4];
        private int count = 0;

        public NotificationBoxCommand(string label, Action act0)
        {
            this.label = label;
            this.act0 = act0;
            this.count = 0;
        }

        public NotificationBoxCommand(string label, Action<Object> act1, Object arg1)
        {
            this.label = label;
            this.act1 = act1;
            this.args[0] = arg1;
            this.count = 1;
        }

        public NotificationBoxCommand(string label, Action<Object, Object> act2, Object arg1, Object arg2)
        {
            this.label = label;
            this.act2 = act2;
            this.args[0] = arg1;
            this.args[1] = arg2;
            this.count = 2;
        }

        public NotificationBoxCommand(string label, Action<Object, Object, Object> act3, Object arg1, Object arg2, Object arg3)
        {
            this.label = label;
            this.act3 = act3;
            this.args[0] = arg1;
            this.args[1] = arg2;
            this.args[2] = arg3;
            this.count = 3;
        }

        public NotificationBoxCommand(string label, Action<Object, Object, Object, Object> act4, Object arg1, Object arg2, Object arg3, Object arg4)
        {
            this.label = label;
            this.act4 = act4;
            this.args[0] = arg1;
            this.args[1] = arg2;
            this.args[2] = arg3;
            this.args[3] = arg4;
            this.count = 4;
        }

        public string Label()
        {
            return this.label;
        }

        public void DoAct()
        {
            switch (this.count)
            {
                case 0:
                    this.act0();
                    break;
                case 1:
                    this.act1(this.args[0]);
                    break;
                case 2:
                    this.act2(this.args[0], this.args[1]);
                    break;
                case 3:
                    this.act3(this.args[0], this.args[1], this.args[2]);
                    break;
                case 4:
                    this.act4(this.args[0], this.args[1], this.args[2], this.args[3]);
                    break;
            }
        }
    }

    internal class NotificationBoxArgs
    {
        public string title;
        public string text;
        public NotificationBoxCommand[] commands;

        public NotificationBoxArgs(string title, string text, NotificationBoxCommand[] commands)
        {
            this.title = title;
            this.text = text;
            this.commands = commands;
        }
    }

    internal class NotificationBox
    {
        private static bool _shown = false;
        private static MessageDialog _msgbox = null;
        private static IUICommand _cmd = null;
        private static List<NotificationBoxArgs> _list = new List<NotificationBoxArgs>();

        public static bool IsOpen()
        {
            return _shown;
        }

        public static void Show(string title, string text, params NotificationBoxCommand[] commands)
        {
            try
            {
                if (!_shown)
                    ShowHelper(title, text, commands);
                else
                    _list.Add(new NotificationBoxArgs(title, text, commands));
            }
            catch (Exception)
            {
            }
        }

        private static void CheckList()
        {
            if (_list.Count > 0)
            {
                var elm = _list[0];
                string title = elm.title;
                string text = elm.text;
                NotificationBoxCommand[] commands = elm.commands;
                _list.RemoveAt(0);
                Show(title, text, commands);
            }
        }

        private async static void ShowHelper(string title, string text, NotificationBoxCommand[] commands)
        {
            _msgbox = new MessageDialog(text);
            _msgbox.Title = title;
            if (commands != null)
                foreach (var cmd in commands)
                    _msgbox.Commands.Add(
                        new UICommand(cmd.Label(), (UICommandInvokedHandler) => { cmd.DoAct(); _shown = false; }));
            else
                _msgbox.Commands.Add(
                    new UICommand(Labels.CloseMessage, (UICommandInvokedHandler) => { _shown = false; }));
            _shown = true;
            _cmd = await _msgbox.ShowAsync();
            CheckList();
        }
    }
}
