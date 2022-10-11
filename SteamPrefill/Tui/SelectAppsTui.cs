using Color = Terminal.Gui.Color;
using Attribute = Terminal.Gui.Attribute;

namespace SteamPrefill.Tui
{
    //TODO spend some time cleaning up this code
    //TODO selected rows need some more coloring to differentiate what is selected
    //TODO implement sorting by purchase date
    //TODO need to document in readme how you navigate the ui.  Can use keyboard alt + shift alt.  Or click with a mouse
    public partial class SelectAppsTui
    {
        private ListView _listView;
        private TextField _searchBox;
        private StatusBar _statusBar;
        private Label headerLabel;

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

            headerLabel.Text = ListViewDataSource.FormatHeaderString();
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

        private void SortPlaytime_OnClicked()
        {
            ListViewDataSource.SortPlaytime();
            _listView.SetNeedsDisplay();
        }

        private void SortSelected_OnClicked()
        {
            ListViewDataSource.SortSelected();
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
}
