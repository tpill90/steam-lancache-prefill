using NStack;
using Terminal.Gui;
using Color = Terminal.Gui.Color;
using Attribute = Terminal.Gui.Attribute;

namespace SteamPrefill.CliCommands
{
    //TODO Enter to finish selection
    //TODO Switching between sorting methods doesn't default to descending
    //TODO Finish search implementation
    //TODO implement sorting by recently played games
    //TODO include more metadata in the list view, like year/minutes played/last played/etc
    //TODO selected rows need some more coloring to differentiate what is selected
    //TODO the first list item should always be the first thing selected when starting up the Tui
    //TODO implement scroll bar
    //TODO Can I format the list items to show checked with [X] like the original
    //TODO can the selected items check show as blue
    //TODO get rid of ugly green outline
    //TODO this needs to be tested on Cmd/Windows Terminal/Linux
    //TODO Search Box - control+a in text box needs to select all text
    //TODO Search Box - Need a way to easily clear current query
    //TODO update readme with new pictures
    //TODO it looks like the year being displayed is not the actual original release year, but rather when the game was added to steam.  Look at the EA games
    public partial class SelectAppsTui
    {
        private ListView _listView;
        private AppInfoDataSource _listViewDataSource => ((AppInfoDataSource)_listView.Source);

        private TextField searchBox;

        public Toplevel top;

        readonly ColorScheme _elementColorScheme = new ColorScheme
        {
            Normal = new Attribute(foreground: Color.White, background: Color.Black),
            HotNormal = new Attribute(foreground: Color.White, background: Color.Black),
            Focus = new Attribute(foreground: Color.BrightBlue, background: Color.Black),
            HotFocus = new Attribute(foreground: Color.BrightBlue, background: Color.Black),
        };

        public SelectAppsTui(List<AppInfo> appInfos)
        {
            //TODO what is the correct encoding to use
            Console.OutputEncoding = Encoding.Default;
            //TODO determine the correct console
            Application.UseSystemConsole = false;

            InitLayout(appInfos);
        }

        public void Run()
        {
            Application.Run(top);
            Application.Shutdown();
        }


        //TODO determine if this dispose is required
        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "<Pending>")]
        private void InitLayout(List<AppInfo> appInfos)
        {
            top = Application.Top;

            var window = new Window("")
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                ColorScheme = Colors.TopLevel
                //ColorScheme = new ColorScheme()
                //{
                //    Normal = new Attribute(Color.White, Color.Black)
                //}
            };
            top.Add(window);

            SetupFirstRow(window);

            var searchLabel = new Label("Search: ")
            {
                X = 1,
                Y = 3
            };
            window.Add(searchLabel);

            searchBox = new TextField()
            {
                X = Pos.Right(searchLabel) + 1,
                Y = 3,
                Width = 50
            };
            searchBox.TextChanged += SearchBoxOnTextChanged;
            window.Add(searchBox);

            _listView = new ListView
            {
                X = 1,
                Y = 5,
                Height = Dim.Fill(),
                Width = Dim.Fill(1),
                ColorScheme = new ColorScheme()
                {
                    Normal = new Attribute(foreground: Color.Gray, background: Color.Black),
                    HotNormal = new Attribute(foreground: Color.Gray, background: Color.Black),
                    Focus = new Attribute(foreground: Color.Cyan, background: Color.Black),
                },
                AllowsMarking = true,
                AllowsMultipleSelection = true,

            };
            _listView.RowRender += ListView_RowRender;
            _listView.Source = new AppInfoDataSource(appInfos);
            window.Add(_listView);

            //TODO should the different actions have different colors?
            var statusBar = new StatusBar
            {
                Visible = true,
                ColorScheme = new ColorScheme
                {
                    Normal = new Attribute(Color.White, Color.Black),
                    HotNormal = new Attribute(foreground: Color.BrightGreen, background: Color.Black),
                }
            };
            statusBar.Items = new StatusItem[] {
                new StatusItem(Key.Esc, "~ESC~ to Quit", () =>
                {
                    Application.RequestStop(top);
                    top.SetNeedsDisplay();
                }),
                new StatusItem (Key.CharMask, "~↑/↓/PgUp/PgDn~ to navigate", null),
                new StatusItem (Key.CharMask, "~Space~ to select", null),
                new StatusItem (Key.CtrlMask | Key.A, "~CTRL-A~ Select All", () =>
                {
                    _listViewDataSource.SetAllSelected(true);
                    _listView.SetNeedsDisplay();
                }),
                new StatusItem (Key.CtrlMask | Key.C, "~CTRL-C~ Clear All", () =>
                {
                    _listViewDataSource.SetAllSelected(false);
                    _listView.SetNeedsDisplay();
                }),
                new StatusItem (Key.Enter, "~Enter~ to Save", () =>
                {
                    //TODO implement save
                }),
            };
            top.Add(statusBar);


        }

        //TODO determine if this dispose is required
        private void SetupFirstRow(Window window)
        {
            //TODO Do I really need to pass the color scheme to each element?
            var sortLabel = new Label("Sort:")
            {
                X = 1,
                ColorScheme = _elementColorScheme
            };
            var sortNameButton = new Button("Name")
            {
                X = Pos.Right(sortLabel) + 1,
                ColorScheme = _elementColorScheme
            };
            sortNameButton.Clicked += SortNameButtonOnClicked;

            var sortYearButton = new Button("Year")
            {
                X = Pos.Right(sortNameButton) + 1,
                ColorScheme = _elementColorScheme
            };
            sortYearButton.Clicked += SortYearButtonOnClicked;

            window.Add(sortLabel, sortNameButton, sortYearButton);
        }

        private void SearchBoxOnTextChanged(ustring obj)
        {
            var searchText = searchBox.Text.ToString();
            _listViewDataSource.FilterItems(searchText);
            _listView.SetNeedsDisplay();
        }

        private void SortNameButtonOnClicked()
        {
            ((AppInfoDataSource)_listView.Source).SortName();
            _listView.SetNeedsDisplay();
        }

        private void SortYearButtonOnClicked()
        {
            ((AppInfoDataSource)_listView.Source).SortYear();
            _listView.SetNeedsDisplay();
        }

        private void ListView_RowRender(ListViewRowEventArgs obj)
        {
            //TODO implement
            //if (obj.Row == _listView.SelectedItem)
            //{
            //    return;
            //}
            //if (_listView.AllowsMarking && _listView.Source.IsMarked(obj.Row))
            //{
            //    //TODO use this to color the rows or something
            //    //obj.RowAttribute = new Attribute(Color.BrightRed, Color.BrightYellow);
            //    return;
            //}
        }
    }
}
