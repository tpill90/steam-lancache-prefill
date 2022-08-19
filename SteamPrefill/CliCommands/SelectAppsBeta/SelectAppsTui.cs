using System.Collections;
using System.Text;
using NStack;
using Terminal.Gui;
using Color = Terminal.Gui.Color;
using Attribute = Terminal.Gui.Attribute;
using Rune = System.Rune;

namespace SteamPrefill.CliCommands.SelectAppsBeta
{
    //TODO implement sorting by recently played games
    //TODO include more metadata in the list view, like year/minutes played/last played/etc
    //TODO selected rows need some more coloring to differentiate what is selected
    //TODO get rid of ugly green outline
    //TODO it looks like the year being displayed is not the actual original release year, but rather when the game was added to steam.  Look at the EA games
    public partial class SelectAppsTui
    {
        private ListView _listView;
        private TextField _searchBox;
        private StatusBar _statusBar;

        private AppInfoDataSource ListViewDataSource => (AppInfoDataSource)_listView.Source;

        readonly ColorScheme _buttonColorScheme = new ColorScheme
        {
            Normal = new Attribute(foreground: Color.White, background: Color.Black),
            HotNormal = new Attribute(foreground: Color.White, background: Color.Black),
            Focus = new Attribute(foreground: Color.BrightBlue, background: Color.Black),
            HotFocus = new Attribute(foreground: Color.BrightBlue, background: Color.Black),
        };

        public SelectAppsTui(List<AppInfo> availableGames, SteamManager steamManager)
        {
            //TODO what is the correct encoding to use
            Console.OutputEncoding = Encoding.Default;
            //TODO determine the correct console
            Application.UseSystemConsole = false;

            InitLayout(availableGames);

            // Restoring previously selected items
            foreach (var id in steamManager.LoadPreviouslySelectedApps())
            {
                var appInfo = availableGames.FirstOrDefault(e => e.AppId == id);
                if (appInfo != null)
                {
                    appInfo.IsSelected = true;
                }
            }

            // Configuring status bar actions
            _statusBar.Items = new StatusItem[] {
                new StatusItem(Key.Esc, "~ESC~ to Quit", () =>
                {
                    Application.RequestStop(Application.Top);
                    Application.Top.SetNeedsDisplay();
                }),
                new StatusItem (Key.CharMask, "~↑/↓/PgUp/PgDn~ to navigate", null),
                new StatusItem (Key.CharMask, "~Space~ to select", null),
                new StatusItem (Key.CtrlMask | Key.A, "~CTRL-A~ Select All", () =>
                {
                    ListViewDataSource.SetAllSelected(true);
                    _listView.SetNeedsDisplay();
                }),
                new StatusItem (Key.CtrlMask | Key.C, "~CTRL-C~ Clear All", () =>
                {
                    ListViewDataSource.SetAllSelected(false);
                    _listView.SetNeedsDisplay();
                }),
                new StatusItem (Key.Enter, "~Enter~ to Save", () =>
                {
                    steamManager.SetAppsAsSelected(ListViewDataSource.SelectedApps);
                    Application.RequestStop(Application.Top);
                    Application.Top.SetNeedsDisplay();
                })
            };
        }

        public void Run()
        {
            _searchBox.SetFocus();
            Application.Run(Application.Top);
            Application.Shutdown();
        }

        private void SearchBox_OnTextChanged(ustring obj)
        {
            var searchText = _searchBox.Text.ToString();

            ListViewDataSource.FilterItems(searchText);
            _listView.MoveHome();
            _listView.SetNeedsDisplay();
        }

        private void SortName_OnClicked()
        {
            ListViewDataSource.SortName();
            _listView.SetNeedsDisplay();
        }

        private void SortYear_OnClicked()
        {
            ListViewDataSource.SortYear();
            _listView.SetNeedsDisplay();
        }

        private void ListView_RowRender(ListViewRowEventArgs obj)
        {
            if (obj.Row == _listView.SelectedItem && _listView.HasFocus)
            {
                obj.RowAttribute = new Attribute(Color.BrightCyan, Color.Black);
                return;
            }
            if (_listView.Source.IsMarked(obj.Row))
            {
                obj.RowAttribute = new Attribute(Color.BrightYellow, Color.Black);
            }
        }
    }

    internal class AppInfoDataSource : IListDataSource
    {
        public int Count => _currAppInfo != null ? _currAppInfo.Count : 0;

        private int _maxItemLength;
        public int Length => _maxItemLength;

        private List<AppInfo> _originalAppInfo;
        private List<AppInfo> _currAppInfo;

        public List<AppInfo> SelectedApps => _originalAppInfo.Where(e => e.IsSelected).ToList();

        public AppInfoDataSource(List<AppInfo> itemList)
        {
            _currAppInfo = itemList;
            _originalAppInfo = itemList.ToList();
            _maxItemLength = _currAppInfo.Select(e => FormatItemString(e).Length).Max();
        }

        public bool IsMarked(int item)
        {
            if (item >= 0 && item < _currAppInfo.Count)
            {
                return _currAppInfo[item].IsSelected;
            }

            return false;
        }

        public void SetMark(int item, bool value)
        {
            if (item >= 0 && item < _currAppInfo.Count)
            {
                _currAppInfo[item].IsSelected = value;
            }
        }

        //TODO comment
        public void SetAllSelected(bool isSelected)
        {
            for (int i = 0; i < _currAppInfo.Count; i++)
            {
                _currAppInfo[i].IsSelected = isSelected;
            }
        }

        public void FilterItems(string searchText)
        {
            var currAppInfo = _originalAppInfo.Where(e => e.Name.Contains(searchText, StringComparison.InvariantCultureIgnoreCase)).ToList();
            _currAppInfo = currAppInfo;
        }

        public void SortName()
        {
            _currAppInfo = _originalAppInfo.OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private bool _sortYearToggle;
        public void SortYear()
        {
            if (_sortYearToggle)
            {
                _currAppInfo = _currAppInfo.OrderBy(e => e.SteamReleaseDate).ToList();
            }
            else
            {
                _currAppInfo = _currAppInfo.OrderByDescending(e => e.SteamReleaseDate).ToList();
            }
            _sortYearToggle = !_sortYearToggle;
        }


        //TODO better name
        private string FormatItemString(AppInfo item)
        {
            var nameColumn = string.Format($"{{0,{-45}}}", item.Name);
            var formattedReleaseDate = string.Format($"{{0,{-20}}}", item.SteamReleaseDate?.Date.ToString("yyyy"));
            var formattedType = item.Type.Name;

            return $"{nameColumn} {formattedReleaseDate} {formattedType}";
        }

        public void Render(ListView container, ConsoleDriver driver, bool selected, int item, int col, int line, int width, int start = 0)
        {
            container.Move(col, line);
            RenderUstr(driver, FormatItemString(_currAppInfo[item]), col, line, width, start);
        }

        // A slightly adapted method from: https://github.com/gui-cs/Terminal.Gui/blob/fc1faba7452ccbdf49028ac49f0c9f0f42bbae91/Terminal.Gui/Views/ListView.cs#L433-L461
        //TODO not entirely sure why this is here.  Could possibly be an extension method?
        private void RenderUstr(ConsoleDriver driver, ustring ustr, int col, int line, int width, int start = 0)
        {
            int used = 0;
            int index = start;
            while (index < ustr.Length)
            {
                (var rune, var size) = Utf8.DecodeRune(ustr, index, index - ustr.Length);
                var count = Rune.ColumnWidth(rune);
                if (used + count >= width) break;
                driver.AddRune(rune);
                used += count;
                index += size;
            }

            while (used < width)
            {
                driver.AddRune(' ');
                used++;
            }
        }

        public IList ToList()
        {
            return _originalAppInfo;
        }
    }
}
