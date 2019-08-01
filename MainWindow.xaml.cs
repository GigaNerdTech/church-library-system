using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Data.SQLite;
using System.Text.RegularExpressions;
using System.IO;
using Microsoft.Win32;

namespace Church_Library_System
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();

            // Load settings
            SQLiteConnection settingLoad = new SQLiteConnection("Data Source=LibraryDatabase.sqlite;Version=3;datetimeformat=CurrentCulture");
            settingLoad.Open();

            string settingLoadCommand = "SELECT * FROM SettingsDatabase";
            SQLiteCommand loadSettingsCommand = new SQLiteCommand(settingLoadCommand, settingLoad);
            try
            {
                SQLiteDataReader reader = loadSettingsCommand.ExecuteReader();


                if (Convert.ToString(reader) != "")
                {
                    SettingsValues.DefaultDays = Int32.Parse(reader["DefaultDays"].ToString());
                    SettingsValues.DefaultFine = float.Parse(reader["DefaultFine"].ToString());
                    SettingsValues.MaxItems = Int32.Parse(reader["MaxItems"].ToString());
                    SettingsValues.AllowFineOverride = (reader["AllowFineOverride"] as int?) == 1;
                    SettingsValues.ChurchName = reader["ChurchName"].ToString();
                }
                else
                {
                    MessageBox.Show("Settings not found. Please initialize databases.");
                    MainTabControl.SelectedIndex = 6;
                }
            }
            catch
            {
                MessageBox.Show("Settings not found. Please initialize databases.");
                MainTabControl.SelectedIndex = 6;
                this.InvalidateVisual();
                return;
            }
            settingLoad.Close();

            //Fill out settings tab
            DefaulDaysBox.Text = SettingsValues.DefaultDays.ToString();
            DefaultFineBox.Text = SettingsValues.DefaultFine.ToString();
            MaxItemsBox.Text = SettingsValues.MaxItems.ToString();
            AllowFineOverrideCheckbox.IsChecked = SettingsValues.AllowFineOverride;
            ChurchNameBox.Text = SettingsValues.ChurchName;

            // Remove Allow Fine Override if setting disabled
            if (SettingsValues.AllowFineOverride == false)
            {
                FineOverrideCheckbox.IsEnabled = false;
            }
            this.Title = SettingsValues.ChurchName + " Library System";
            MainTabControl.SelectedIndex = 4;
            TitleRadioButton.IsChecked = true;
            DatabaseControlCheckbox.IsChecked = false;
            ClearItemDBButton.IsEnabled = false;
            ClearPatronDBButton.IsEnabled = false;
            ImportItemButton.IsEnabled = false;
            ViewPatronDBButton.IsEnabled = false;
            ClearPatronDBButton.IsEnabled = false;
            ClearItemDBButton.IsEnabled = false;
            InitializeDBButton.IsEnabled = false;
            this.InvalidateVisual();

        }

        private void InitializeDBButton_Click(object sender, RoutedEventArgs e)
        {
            //Create the database file
            SQLiteConnection createDB;
            SQLiteConnection.CreateFile("LibraryDatabase.sqlite");

            //Open a connection
            createDB = new SQLiteConnection("Data Source=LibraryDatabase.sqlite;Version=3;");
            createDB.Open();

            //Create the item database
            string ItemDBCommand = "CREATE TABLE ItemDatabase (ItemNumber INTEGER PRIMARY KEY AUTOINCREMENT, Author VARCHAR(100), Title VARCHAR(400), DateAcquired DATE, Price DECIMAL(3,2), Classification VARCHAR(10), CallLetters VARCHAR(3), Copyright INTEGER, " + 
                "Publisher VARCHAR(60), Source VARCHAR(10), Extent VARCHAR(30), Remarks VARCHAR(500), ISBN VARCHAR(25), Edition VARCHAR(40), Notes VARCHAR(500), Subject1 VARCHAR(50), Subject2 VARCHAR(50), " + "" +
                "Subject3 VARCHAR(50), Subject4 VARCHAR(50), Subject5 VARCHAR(50), Rate VARCHAR(1), DonorName VARCHAR(80), DonorText1 VARCHAR(80), DonorText2 VARCHAR(80), " +
                "DonationDate DATE, InMemory VARCHAR(1), AgeCode VARCHAR(1), MediaDesignation VARCHAR(2), HonoreeName VARCHAR(100), CheckedOut BOOLEAN, DueDate DATE, PatronID INTEGER)";
            SQLiteCommand createItemDB = new SQLiteCommand(ItemDBCommand, createDB);
            createItemDB.ExecuteNonQuery();


            //Create Patron Database
            string PatronDBCommand = "CREATE TABLE PatronDatabase (PatronID INTEGER PRIMARY KEY AUTOINCREMENT, FirstName VARCHAR(80), LastName VARCHAR(100), StreetAddress VARCHAR(500), City VARCHAR(100), State VARCHAR(2), ZIP INTEGER(5), ChurchMember BOOLEAN, EmailAddress VARCHAR(200), PhoneNumber VARCHAR(10), BirthDate DATE, FinesOwed DECIMAL(2,2))";
            SQLiteCommand createPatronDB = new SQLiteCommand(PatronDBCommand, createDB);
            createPatronDB.ExecuteNonQuery();

            //Create Settings Database
            string SettingsDBCommand = "CREATE TABLE SettingsDatabase (DefaultDays INTEGER, DefaultFine DECIMAL(2,2), MaxItems INTEGER, AllowFineOverride BOOLEAN, ChurchName VARCHAR(200))";
            SQLiteCommand createSettingsDB = new SQLiteCommand(SettingsDBCommand, createDB);
            createSettingsDB.ExecuteNonQuery();

            //Create History Database
            string HistoryDBCommand = "CREATE TABLE HistoryDatabase (TransactionID INTEGER PRIMARY KEY AUTOINCREMENT, ItemID INTEGER, PatronID INTEGER, CheckOutDate DATETIME, CheckInDate DATETIME)";
            SQLiteCommand createHistoryDB = new SQLiteCommand(HistoryDBCommand, createDB);
            createHistoryDB.ExecuteNonQuery();


            // Set defaults
            string SettingsDBDefaultConfigCommand = "INSERT INTO SettingsDatabase VALUES (14, 0.20, 10, 1, \"Fairview Baptist Church\")";
            SQLiteCommand defaultSettings = new SQLiteCommand(SettingsDBDefaultConfigCommand, createDB);
            defaultSettings.ExecuteNonQuery();

            string addCheckOutItemCommand = "CREATE TABLE CheckOutList (ItemID INTEGER, Title VARCHAR(400), Author VARCHAR(100), Subject VARCHAR(50), CallNo VARCHAR(13))";
            SQLiteCommand addCheckOutItemDB = new SQLiteCommand(addCheckOutItemCommand, createDB);
            addCheckOutItemDB.ExecuteNonQuery();

            

            //Load settings
            string settingLoadCommand = "SELECT * FROM SettingsDatabase";
            SQLiteCommand loadSettingsCommand = new SQLiteCommand(settingLoadCommand, createDB);

            SQLiteDataReader reader = loadSettingsCommand.ExecuteReader();
            SettingsValues.DefaultDays = Int32.Parse(reader["DefaultDays"].ToString());
            SettingsValues.DefaultFine = float.Parse(reader["DefaultFine"].ToString());
            SettingsValues.MaxItems = Int32.Parse(reader["MaxItems"].ToString());
            SettingsValues.AllowFineOverride = (reader["AllowFineOverride"] as int?) == 1;
            SettingsValues.ChurchName = reader["ChurchName"].ToString();

            //Close connection
            createDB.Close();

            //Show Confirmation dialog
            string createDBMessage = "Databases created!";
            MessageBox.Show(createDBMessage);

        }

        private void AddItemButton_Click(object sender, RoutedEventArgs e)
        {

            //Open Database connection
            SQLiteConnection addItem = new SQLiteConnection("Data Source=LibraryDatabase.sqlite;Version=3;datetimeformat=CurrentCulture");
            addItem.Open();

            //Break up subject field
            string[] Subjects;
            string[] destSubjects = new string[5];

            Subjects = ItemSubjectsBox.Text.Split(',');

            
            int counter = 0;
            foreach (string sub in Subjects)
            {
                Subjects[counter] = "\"" + Subjects[counter].Trim() + "\"";
                counter++;
            }

            Array.Copy(Subjects, 0, destSubjects, 0, counter);

            for (int i = counter; i < 5; i++)
            {
                destSubjects[i] = "NULL";
            }

            //Break up Donor text field
            string[] DonorText;
            string[] destDonorText = new string[2];

            DonorText = ItemDonorNotesBox.Text.Split(',');
            counter = 0;
            if (DonorText[0] != "")
            {
                
                foreach (string donor in DonorText)
                {
                    DonorText[counter] = "\"" + DonorText[counter].Trim() + "\"";
                    counter++;
                }

                Array.Copy(DonorText, 0, destDonorText, 0, counter);
            }
            for (int i = counter; i < 2; i++)
            {
                destDonorText[i] = "NULL";
            }
            //Assign variables for SQL statement
            string Title = ItemTitleBox.Text.Trim();
            string Author = ItemAuthorBox.Text.Trim();
            string DateAcquired = ItemDateAcquiredPicker.Text.Trim();
            string Price = ItemPriceBox.Text.Trim();
            string Class = ItemClassBox.Text.Trim();
            string CallNo = ItemCallNoBox.Text.Trim();
            string Copyright = ItemCopyrightBox.Text.Trim();
            string Publisher = ItemPublisherBox.Text.Trim();
            string Source = ItemSourceBox.Text.Trim();
            string Extent = ItemExtentBox.Text.Trim();
            string Remarks = ItemRemarksBox.Text.Trim();
            string ISBN = ItemISBNBox.Text.Trim();
            string Edition = ItemEditionBox.Text.Trim();
            string Notes = ItemNotesBox.Text.Trim();
            string Rate = ItemRateBox.Text.Trim();
            string DonorName = ItemDonorNameBox.Text.Trim();
            string DonationDate = ItemDonationDatePicker.Text.Trim();
            string InMemory = ItemInMemoryBox.Text.Trim();
            string AgeCode = ItemAgeCodeBox.Text.Trim();
            string MediaType = ItemMediaTypeBox.Text.Trim();
            string HonoreeName = ItemHonoreeName.Text.Trim();

            //Validate required data
            if (Title == "")
            {
                MessageBox.Show("Title required!");
                return;
            }
            else
            {
                Title = "\"" + Title + "\"";
            }

            if (Author == "")
            {
                MessageBox.Show("Author required!");
                return;
            }
            else
            {
                Author = "\"" + Author + "\"";
            }
            if (CallNo == "")
            {
                MessageBox.Show("Call Number required!");
                return;
            }
            else
            {
                CallNo = "\"" + CallNo + "\"";
            }

            if (destSubjects[0] == "NULL")
            {
                MessageBox.Show("At least one subject required!");
                return;
            }

            // Replace blank data with NULLs for SQL
            if (DateAcquired == "")
            {
                DateAcquired = "NULL";
            }
            else
            {
                DateAcquired = "\"" + DateAcquired + "\"";
            }
            if (Price == "")
            {
                Price = "NULL";
            }
            else
            {
                Price = "\"" + Price + "\"";
            }
            if (Class == "")
            {
                Class = "NULL";
            }
            else
            {
                Class = "\"" + Class + "\"";
            }
            if (Copyright == "")
            {
                Copyright = "NULL";
            }
            else
            {
                Copyright = "\"" + Copyright + "\"";
            }
            if (Publisher == "")
            {
                Publisher = "NULL";
            }
            else
            {
                Publisher = "\"" + Publisher + "\"";
            }
            if (Source == "")
            {
                Source = "NULL";
            }
            else
            {
                Source = "\"" + Source + "\"";
            }
            if (Extent == "")
            {
                Extent = "NULL";
            }
            else
            {
                Extent = "\"" + Extent + "\"";
            }
            if (Remarks == "")
            {
                Remarks = "NULL";
            }
            else
            {
                Remarks = "\"" + Remarks + "\"";
            }
            if (ISBN == "")
            {
                ISBN = "NULL";
            }
            else
            {
                ISBN = "\"" + ISBN + "\"";
            }
            if (Edition == "")
            {
                Edition = "NULL";
            }
            else
            {
                Edition = "\"" + Edition + "\"";
            }
            if (Notes == "")
            {
                Notes = "NULL";
            }
            else
            {
                Notes = "\"" + Notes + "\"";
            }
            if (Rate == "")
            {
                Rate = "NULL";
            }
            else
            {
                Rate = "\"" + Rate + "\"";
            }
            if (DonorName == "")
            {
                DonorName = "NULL";
            }
            else
            {
                DonorName = "\"" + DonorName + "\"";
            }
            if (DonationDate == "")
            {
                DonationDate = "NULL";
            }
            else
            {
                DonationDate = "\"" + DonationDate + "\"";
            }
            if (InMemory == "")
            {
                InMemory = "NULL";
            }
            else
            {
                InMemory = "\"" + InMemory + "\"";
            }
            if (AgeCode == "")
            {
                AgeCode = "NULL";
            }
            else
            {
                AgeCode = "\"" + AgeCode + "\"";
            }
            if (MediaType == "")
            {
                MediaType = "NULL";
            }
            else
            {
                MediaType = "\"" + MediaType + "\"";
            }
            if (HonoreeName == "")
            {
                HonoreeName = "NULL";
            }
            else
            {
                HonoreeName = "\"" + HonoreeName + "\"";
            }


            //Build Database command
            string addItemCommand = "INSERT INTO ItemDatabase VALUES (NULL, " + Author + ", " + Title + ", " + DateAcquired + ", " + Price + ", " + Class + ", " + CallNo + ", " + Copyright + ", " + Publisher + ", " +
                Source + ", " + Extent + ", " + Remarks + ", " + ISBN + ", " + Edition + ", " + Notes + ", " + destSubjects[0] + ", " + destSubjects[1] + ", " + destSubjects[2] + ", " + destSubjects[3] + ", " + destSubjects[4] + ", " +
                Rate + ", " + DonorName + ", " + destDonorText[0] + ", " + destDonorText[1] + ", " + DonationDate + ", " + InMemory + ", " + AgeCode + ", " + MediaType + ", " + HonoreeName + ", 0, NULL, NULL)";

            SQLiteCommand addItemToDB = new SQLiteCommand(addItemCommand, addItem);
            addItemToDB.ExecuteNonQuery();
            addItem.Close();


            MessageBox.Show("Item added!");

            //Clear all fields
            ItemTitleBox.Text = "";
            ItemAuthorBox.Text = "";
            ItemDateAcquiredPicker.Text = "";
            ItemPriceBox.Text = "";
            ItemClassBox.Text = "";
            ItemCallNoBox.Text = "";
            ItemCopyrightBox.Text = "";
            ItemPublisherBox.Text = "";
            ItemSourceBox.Text = "";
            ItemExtentBox.Text = "";
            ItemRemarksBox.Text = "";
            ItemISBNBox.Text = "";
            ItemEditionBox.Text = "";
            ItemNotesBox.Text = "";
            ItemRateBox.Text = "";
            ItemDonorNameBox.Text = "";
            ItemDonationDatePicker.Text = "";
            ItemInMemoryBox.Text = "";
            ItemAgeCodeBox.Text = "";
            ItemMediaTypeBox.Text = "";
            ItemHonoreeName.Text = "";
            ItemSubjectsBox.Text = "";
            ItemDonorNotesBox.Text = "";

            this.InvalidateVisual();
        }

        private void EditPatronButton_Click(object sender, RoutedEventArgs e)
        {
            //Open Database connection
            SQLiteConnection editPatron = new SQLiteConnection("Data Source=LibraryDatabase.sqlite;Version=3;datetimeformat=CurrentCulture");
            editPatron.Open();

            //Assign text box values to local variables
            string FirstName = PatronFirstNameBox.Text.Trim();
            string LastName = PatronLastNameBox.Text.Trim();
            string Address = PatronAddressBox.Text.Trim();
            string City = PatronCityBox.Text.Trim();
            string State = PatronStateBox.Text.Trim();
            string ZIP = PatronZIPBox.Text.Trim();
            string ChurchMember;
            string Email = PatronEmailBox.Text.Trim();
            string Phone = PatronPhoneBox.Text.Trim();
            string BirthDate = BirthDateDatePicker.Text;

            if (PatronMemberCheckbox.IsChecked == true)
            {
                ChurchMember = "1";
            }
            else
            {
                ChurchMember = "0";
            }

            //Validate data
            if (FirstName == "")
            {
                MessageBox.Show("First Name required!");
                return;
            }
            else
            {
                FirstName = "\"" + FirstName + "\"";
            }
            if (LastName == "")
            {
                MessageBox.Show("Last Name required!");
                return;
            }
            else
            {
                LastName = "\"" + LastName + "\"";
            }
            if (Address == "")
            {
                MessageBox.Show("Street address required!");
                return;
            }
            else
            {
                Address = "\"" + Address + "\"";
            }
            if (City == "")
            {
                MessageBox.Show("City required!");
                return;
            }
            else
            {
                City = "\"" + City + "\"";
            }
            if (State == "")
            {
                MessageBox.Show("State required!");
                return;
            }
            else
            {
                State = "\"" + State + "\"";
            }
            if (ZIP == "")
            {
                MessageBox.Show("ZIP Code required!");
                return;
            }
            else
            {
                ZIP = "\"" + ZIP + "\"";
            }
            if (BirthDate == "")
            {
                MessageBox.Show("Birth Date required!");
                return;
            }
            else
            {
                BirthDate = "\"" + BirthDate + "\"";
            }

            if (Email == "")
            {
                Email = "NULL";
            }
            else
            {
                Email = "\"" + Email + "\"";
            }
            if (Phone == "")
            {
                Phone = "NULL";
            }
            else
            {
                Phone = "\"" + Phone + "\"";
            }

            string editPatronCommand = "UPDATE PatronDatabase SET FirstName = " + FirstName + ", LastName = " + LastName + ", StreetAddress = " + Address + ", City = " + City + ", State = " + State + ", ZIP = " + ZIP + ", ChurchMember = " + ChurchMember + ", EmailAddress = " + Email + ", PhoneNumber = " + Phone + ", BirthDate = " + BirthDate + " WHERE PatronID = " + CurrentID.CurrentPatronID;
            SQLiteCommand editPatronToDB = new SQLiteCommand(editPatronCommand, editPatron);
            editPatronToDB.ExecuteNonQuery();
            editPatron.Close();

            MessageBox.Show("Patron updated!");

            //Clear form
            PatronFirstNameBox.Text = "";
            PatronLastNameBox.Text = "";
            PatronAddressBox.Text = "";
            PatronCityBox.Text = "";
            PatronStateBox.Text = "";
            PatronZIPBox.Text = "";
            PatronMemberCheckbox.IsChecked = false;
            PatronEmailBox.Text = "";
            PatronPhoneBox.Text = "";
            PatronCurrentItems.ItemsSource = null;
            BirthDateDatePicker.Text = "";
            this.InvalidateVisual();
        }

        private void AddPatronButton_Click(object sender, RoutedEventArgs e)
        {
            //Open Database connection
            SQLiteConnection addPatron = new SQLiteConnection("Data Source=LibraryDatabase.sqlite;Version=3;datetimeformat=CurrentCulture");
            addPatron.Open();

            //Assign text box values to local variables
            string FirstName = PatronFirstNameBox.Text.Trim();
            string LastName = PatronLastNameBox.Text.Trim();
            string Address = PatronAddressBox.Text.Trim();
            string City = PatronCityBox.Text.Trim();
            string State = PatronStateBox.Text.Trim();
            string ZIP = PatronZIPBox.Text.Trim();
            string ChurchMember;
            string Email = PatronEmailBox.Text.Trim();
            string Phone = PatronPhoneBox.Text.Trim();
            string BirthDate = BirthDateDatePicker.Text;

            if (PatronMemberCheckbox.IsChecked == true)
            {
                ChurchMember = "1";
            }
            else
            {
                ChurchMember = "0";
            }

            //Validate data
            if (FirstName == "")
            {
                MessageBox.Show("First Name required!");
                return;
            }
            else
            {
                FirstName = "\"" + FirstName + "\"";
            }
            if (LastName == "")
            {
                MessageBox.Show("Last Name required!");
                return;
            }
            else
            {
                LastName = "\"" + LastName + "\"";
            }
            if (Address == "")
            {
                MessageBox.Show("Street address required!");
                return;
            }
            else
            {
                Address = "\"" + Address + "\"";
            }
            if (City == "")
            {
                MessageBox.Show("City required!");
                return;
            }
            else
            {
                City = "\"" + City + "\"";
            }
            if (State == "")
            {
                MessageBox.Show("State required!");
                return;
            }
            else
            {
                State = "\"" + State + "\"";
            }
            if (BirthDate == "")
            {
                MessageBox.Show("Birth Date required!");
                return;
            }
            else
            {
                BirthDate = "\"" + BirthDate + "\"";
            }
            if (ZIP == "")
            {
                MessageBox.Show("ZIP Code required!");
                return;
            }
            else
            {
                ZIP = "\"" + ZIP + "\"";
            }
            if (Email == "")
            {
                Email = "NULL";
            }
            else
            {
                Email = "\"" + Email + "\"";
            }
            if (Phone == "")
            {
                Phone = "NULL";
            }
            else
            {
                Phone = "\"" + Phone + "\"";
            }

            string addPatronCommand = "INSERT INTO PatronDatabase VALUES (NULL, " + FirstName + ", " + LastName + ", " + Address + ", " + City + ", " + State + ", " + ZIP + ", " + ChurchMember + ", " + Email + ", " + Phone + ", " + BirthDate + ", 0)";
            SQLiteCommand addPatronToDB = new SQLiteCommand(addPatronCommand, addPatron);
            addPatronToDB.ExecuteNonQuery();
            addPatron.Close();

            MessageBox.Show("Patron added!");

            PatronFirstNameBox.Text = "";
            PatronLastNameBox.Text = "";
            PatronAddressBox.Text = "";
            PatronCityBox.Text = "";
            PatronStateBox.Text = "";
            PatronZIPBox.Text = "";
            PatronMemberCheckbox.IsChecked = false;
            PatronEmailBox.Text = "";
            PatronPhoneBox.Text = "";
            BirthDateDatePicker.Text = "";
            this.InvalidateVisual();


        }

        private void EditItemButton_Click(object sender, RoutedEventArgs e)
        {
            //Open Database connection
            SQLiteConnection editItem = new SQLiteConnection("Data Source=LibraryDatabase.sqlite;Version=3;datetimeformat=CurrentCulture");
            editItem.Open();

            //Break up subject field
            string[] Subjects;
            string[] destSubjects = new string[5];

            Subjects = ItemSubjectsBox.Text.Split(',');


            int counter = 0;
            foreach (string sub in Subjects)
            {
                Subjects[counter] = "\"" + Subjects[counter].Trim() + "\"";
                counter++;
            }

            Array.Copy(Subjects, 0, destSubjects, 0, counter);

            for (int i = counter; i < 5; i++)
            {
                destSubjects[i] = "NULL";
            }

            //Break up Donor text field
            string[] DonorText;
            string[] destDonorText = new string[2];

            DonorText = ItemDonorNotesBox.Text.Split(',');
            counter = 0;
            if (DonorText[0] != "")
            {

                foreach (string donor in DonorText)
                {
                    DonorText[counter] = "\"" + DonorText[counter].Trim() + "\"";
                    counter++;
                }

                Array.Copy(DonorText, 0, destDonorText, 0, counter);
            }
            for (int i = counter; i < 2; i++)
            {
                destDonorText[i] = "NULL";
            }
            //Assign variables for SQL statement
            string Title = ItemTitleBox.Text.Trim();
            string Author = ItemAuthorBox.Text.Trim();
            string DateAcquired = ItemDateAcquiredPicker.Text.Trim();
            string Price = ItemPriceBox.Text.Trim();
            string Class = ItemClassBox.Text.Trim();
            string CallNo = ItemCallNoBox.Text.Trim();
            string Copyright = ItemCopyrightBox.Text.Trim();
            string Publisher = ItemPublisherBox.Text.Trim();
            string Source = ItemSourceBox.Text.Trim();
            string Extent = ItemExtentBox.Text.Trim();
            string Remarks = ItemRemarksBox.Text.Trim();
            string ISBN = ItemISBNBox.Text.Trim();
            string Edition = ItemEditionBox.Text.Trim();
            string Notes = ItemNotesBox.Text.Trim();
            string Rate = ItemRateBox.Text.Trim();
            string DonorName = ItemDonorNameBox.Text.Trim();
            string DonationDate = ItemDonationDatePicker.Text.Trim();
            string InMemory = ItemInMemoryBox.Text.Trim();
            string AgeCode = ItemAgeCodeBox.Text.Trim();
            string MediaType = ItemMediaTypeBox.Text.Trim();
            string HonoreeName = ItemHonoreeName.Text.Trim();
            string ItemNumber;

            //Validate required data
            if (Title == "")
            {
                MessageBox.Show("Title required!");
                return;
            }
            else
            {
                Title = "\"" + Title + "\"";
            }

            if (Author == "")
            {
                MessageBox.Show("Author required!");
                return;
            }
            else
            {
                Author = "\"" + Author + "\"";
            }
            if (CallNo == "")
            {
                MessageBox.Show("Call Number required!");
                return;
            }
            else
            {
                CallNo = "\"" + CallNo + "\"";
            }

            if (destSubjects[0] == "NULL")
            {
                MessageBox.Show("At least one subject required!");
                return;
            }

            // Replace blank data with NULLs for SQL
            if (DateAcquired == "")
            {
                DateAcquired = "NULL";
            }
            else
            {
                DateAcquired = "\"" + DateAcquired + "\"";
            }
            if (Price == "")
            {
                Price = "NULL";
            }
            else
            {
                Price = "\"" + Price + "\"";
            }
            if (Class == "")
            {
                Class = "NULL";
            }
            else
            {
                Class = "\"" + Class + "\"";
            }
            if (Copyright == "")
            {
                Copyright = "NULL";
            }
            else
            {
                Copyright = "\"" + Copyright + "\"";
            }
            if (Publisher == "")
            {
                Publisher = "NULL";
            }
            else
            {
                Publisher = "\"" + Publisher + "\"";
            }
            if (Source == "")
            {
                Source = "NULL";
            }
            else
            {
                Source = "\"" + Source + "\"";
            }
            if (Extent == "")
            {
                Extent = "NULL";
            }
            else
            {
                Extent = "\"" + Extent + "\"";
            }
            if (Remarks == "")
            {
                Remarks = "NULL";
            }
            else
            {
                Remarks = "\"" + Remarks + "\"";
            }
            if (ISBN == "")
            {
                ISBN = "NULL";
            }
            else
            {
                ISBN = "\"" + ISBN + "\"";
            }
            if (Edition == "")
            {
                Edition = "NULL";
            }
            else
            {
                Edition = "\"" + Edition + "\"";
            }
            if (Notes == "")
            {
                Notes = "NULL";
            }
            else
            {
                Notes = "\"" + Notes + "\"";
            }
            if (Rate == "")
            {
                Rate = "NULL";
            }
            else
            {
                Rate = "\"" + Rate + "\"";
            }
            if (DonorName == "")
            {
                DonorName = "NULL";
            }
            else
            {
                DonorName = "\"" + DonorName + "\"";
            }
            if (DonationDate == "")
            {
                DonationDate = "NULL";
            }
            else
            {
                DonationDate = "\"" + DonationDate + "\"";
            }
            if (InMemory == "")
            {
                InMemory = "NULL";
            }
            else
            {
                InMemory = "\"" + InMemory + "\"";
            }
            if (AgeCode == "")
            {
                AgeCode = "NULL";
            }
            else
            {
                AgeCode = "\"" + AgeCode + "\"";
            }
            if (MediaType == "")
            {
                MediaType = "NULL";
            }
            else
            {
                MediaType = "\"" + MediaType + "\"";
            }
            if (HonoreeName == "")
            {
                HonoreeName = "NULL";
            }
            else
            {
                HonoreeName = "\"" + HonoreeName + "\"";
            }

            //Get ItemNumber for row update
            string getID = "SELECT ItemNumber FROM ItemDatabase WHERE Title = " + Title;
            SQLiteCommand getIDCommand = new SQLiteCommand(getID, editItem);
            SQLiteDataReader reader = getIDCommand.ExecuteReader();
            ItemNumber = reader["ItemNumber"].ToString();

            //Build Database command
            string editItemCommand = "UPDATE ItemDatabase SET Author = " + Author + ", Title = " + Title + ", DateAcquired = " + DateAcquired + ", Price = " + Price + ", Classification = " + Class + ", CallLetters = " + CallNo + ", Copyright = " + Copyright + ", Publisher = " + Publisher + ", Source = " +
                Source + ", Extent = " + Extent + ", Remarks = " + Remarks + ", ISBN = " + ISBN + ", Edition = " + Edition + ", Notes = " + Notes + ", Subject1 = " + destSubjects[0] + ", Subject2 = " + destSubjects[1] + ", Subject3 = " + destSubjects[2] + ", Subject4 = " + destSubjects[3] + ", Subject5 = " + destSubjects[4] + ", Rate = " +
                Rate + ", DonorName = " + DonorName + ", DonorText1 = " + destDonorText[0] + ", DonorText2 = " + destDonorText[1] + ", DonationDate = " + DonationDate + ", InMemory = " + InMemory + ", AgeCode = " + AgeCode + ", MediaDesignation = " + MediaType + ", HonoreeName = " + HonoreeName + 
                " WHERE ItemNumber = " + CurrentID.CurrentItemID;

            SQLiteCommand editItemToDB = new SQLiteCommand(editItemCommand, editItem);
            editItemToDB.ExecuteNonQuery();
            editItem.Close();


            MessageBox.Show("Item updated!");
            //Clear all fields
            ItemTitleBox.Text = "";
            ItemAuthorBox.Text = "";
            ItemDateAcquiredPicker.Text = "";
            ItemPriceBox.Text = "";
            ItemClassBox.Text = "";
            ItemCallNoBox.Text = "";
            ItemCopyrightBox.Text = "";
            ItemPublisherBox.Text = "";
            ItemSourceBox.Text = "";
            ItemExtentBox.Text = "";
            ItemRemarksBox.Text = "";
            ItemISBNBox.Text = "";
            ItemEditionBox.Text = "";
            ItemNotesBox.Text = "";
            ItemRateBox.Text = "";
            ItemDonorNameBox.Text = "";
            ItemDonationDatePicker.Text = "";
            ItemInMemoryBox.Text = "";
            ItemAgeCodeBox.Text = "";
            ItemMediaTypeBox.Text = "";
            ItemHonoreeName.Text = "";
            ItemSubjectsBox.Text = "";
            ItemDonorNotesBox.Text = "";

            this.InvalidateVisual();
        }

        private void SearchPatronButton_Click(object sender, RoutedEventArgs e)
        {
            string CheckedOutStatus(string DueDate)
            {
                if (DueDate == "True")
                {
                    return "Yes";
                }
                else
                {
                    return "No";
                }
            }

            //Open Database connection
            SQLiteConnection searchPatron = new SQLiteConnection("Data Source=LibraryDatabase.sqlite;Version=3;datetimeformat=CurrentCulture");
            searchPatron.Open();

            string FirstName = PatronFirstNameBox.Text.Trim();
            string LastName = PatronLastNameBox.Text.Trim();

            if (FirstName == "")
            {
                MessageBox.Show("First name required for search!");
                return;
            }

            if (LastName == "")
            {
                MessageBox.Show("Last name required for search!");
                return;
            }


            // Build SQL command

            string patronSearchCommand = "SELECT * FROM PatronDatabase WHERE FirstName LIKE '%" + FirstName + "%' AND LastName LIKE '%" + LastName + "%'";
            SQLiteCommand patronSearch = new SQLiteCommand(patronSearchCommand, searchPatron);

            SQLiteDataReader reader = patronSearch.ExecuteReader();

            CurrentID.CurrentPatronID = reader["PatronID"].ToString().Trim();
            PatronFirstNameBox.Text = reader["FirstName"].ToString();
            PatronLastNameBox.Text = reader["LastName"].ToString();
            PatronAddressBox.Text = reader["StreetAddress"].ToString();
            PatronCityBox.Text = reader["City"].ToString();
            PatronStateBox.Text = reader["State"].ToString();
            PatronZIPBox.Text = reader["ZIP"].ToString();
            BirthDateDatePicker.Text = reader["BirthDate"].ToString();
            FinesOwedLabel.Content = "Fines Owed: " + float.Parse(reader["FinesOwed"].ToString()).ToString("0.00");

            if (reader["ChurchMember"].ToString() == "True")
            {
                PatronMemberCheckbox.IsChecked = true;
            }
            else
            {
                PatronMemberCheckbox.IsChecked = false;
            }
            PatronEmailBox.Text = reader["EmailAddress"].ToString();
            PatronPhoneBox.Text = reader["PhoneNumber"].ToString();


            string itemSearchCommand = "SELECT * FROM ItemDatabase WHERE PatronID = " + CurrentID.CurrentPatronID;
            SQLiteCommand itemSearch = new SQLiteCommand(itemSearchCommand, searchPatron);
            SQLiteDataReader reader_item = itemSearch.ExecuteReader();

            List<ItemData> SearchResults = new List<ItemData>();
            while (reader_item.Read())
            {
                SearchResults.Add(new ItemData()
                {
                    ItemID = reader_item["ItemNumber"].ToString(),
                    Title = reader_item["Title"].ToString(),
                    Author = reader_item["Author"].ToString(),
                    Subject = reader_item["Subject1"].ToString(),
                    CallNo = reader_item["Classification"].ToString() + " " + reader_item["CallLetters"].ToString(),
                    CheckedOut = CheckedOutStatus(reader_item["CheckedOut"].ToString()),
                    DueDate = reader_item["DueDate"].ToString()
                }); ;
            }

            PatronCurrentItems.ItemsSource = SearchResults;

            this.InvalidateVisual();

            searchPatron.Close();

            MessageBox.Show("Patron loaded!");


        }

        private void DeletePatronButton_Click(object sender, RoutedEventArgs e)
        {
            SQLiteConnection deletePatron = new SQLiteConnection("Data Source=LibraryDatabase.sqlite;Version=3;datetimeformat=CurrentCulture");
            deletePatron.Open();


            string deletePatronCommand = "DELETE FROM PatronDatabase WHERE PatronID = " + CurrentID.CurrentPatronID;

            SQLiteCommand deletePatronsql = new SQLiteCommand(deletePatronCommand, deletePatron);
            deletePatronsql.ExecuteNonQuery();

            PatronFirstNameBox.Text = "";
            PatronLastNameBox.Text = "";
            PatronAddressBox.Text = "";
            PatronCityBox.Text = "";
            PatronStateBox.Text = "";
            PatronZIPBox.Text = "";
            PatronMemberCheckbox.IsChecked = false;
            PatronEmailBox.Text = "";
            PatronPhoneBox.Text = "";
            this.InvalidateVisual();

            deletePatron.Close();

            MessageBox.Show("Patron deleted!");

            PatronFirstNameBox.Text = "";
            PatronLastNameBox.Text = "";
            PatronAddressBox.Text = "";
            PatronCityBox.Text = "";
            PatronStateBox.Text = "";
            PatronZIPBox.Text = "";
            PatronMemberCheckbox.IsChecked = false;
            PatronEmailBox.Text = "";
            PatronPhoneBox.Text = "";
            this.InvalidateVisual();

        }

        private void DeleteItemButton_Click(object sender, RoutedEventArgs e)
        {
            SQLiteConnection deleteItem = new SQLiteConnection("Data Source=LibraryDatabase.sqlite;Version=3;datetimeformat=CurrentCulture");
            deleteItem.Open();

            string Title = "\"" + ItemTitleBox.Text + "\"";

            string deleteItemCommand = "DELETE FROM ItemDatabase WHERE ItemNumber = " + CurrentID.CurrentItemID;

            SQLiteCommand deleteItemsql = new SQLiteCommand(deleteItemCommand, deleteItem);
            deleteItemsql.ExecuteNonQuery();

            //Clear all fields
            ItemTitleBox.Text = "";
            ItemAuthorBox.Text = "";
            ItemDateAcquiredPicker.Text = "";
            ItemPriceBox.Text = "";
            ItemClassBox.Text = "";
            ItemCallNoBox.Text = "";
            ItemCopyrightBox.Text = "";
            ItemPublisherBox.Text = "";
            ItemSourceBox.Text = "";
            ItemExtentBox.Text = "";
            ItemRemarksBox.Text = "";
            ItemISBNBox.Text = "";
            ItemEditionBox.Text = "";
            ItemNotesBox.Text = "";
            ItemRateBox.Text = "";
            ItemDonorNameBox.Text = "";
            ItemDonationDatePicker.Text = "";
            ItemInMemoryBox.Text = "";
            ItemAgeCodeBox.Text = "";
            ItemMediaTypeBox.Text = "";
            ItemHonoreeName.Text = "";
            ItemSubjectsBox.Text = "";
            ItemDonorNotesBox.Text = "";

            this.InvalidateVisual();

            deleteItem.Close();

            MessageBox.Show("Item deleted!");
        }

        private void SearchCardCatalogButton_Click(object sender, RoutedEventArgs e)
        {
            string CheckedOutStatus(string DueDate)
            {
                if (DueDate == "True")
                {
                    return "Yes";
                }
                else
                {
                    return "No";
                }
            }
            string getPatron(string patronID)
            {
                if (patronID == "")
                {
                    return "";
                }
                SQLiteConnection getPatronName = new SQLiteConnection("Data Source=LibraryDatabase.sqlite;Version=3;datetimeformat=CurrentCulture");
                getPatronName.Open();

                string getPatronNameCommand = "SELECT FirstName, LastName FROM PatronDatabase WHERE PatronID = " + patronID;
                SQLiteCommand patronIDsql = new SQLiteCommand(getPatronNameCommand, getPatronName);
                SQLiteDataReader name_reader = patronIDsql.ExecuteReader();

                return name_reader["FirstName"].ToString() + " " + name_reader["LastName"].ToString();

            }
            if (SearchCardCatalogBox.Text == "")
            {
                MessageBox.Show("Search Term Required!");
                return;
            }

            string SearchTerm = "'%" + SearchCardCatalogBox.Text + "%'";
            string SearchField = "";
            SQLiteConnection searchCatalog = new SQLiteConnection("Data Source=LibraryDatabase.sqlite;Version=3;datetimeformat=CurrentCulture");
            searchCatalog.Open();

            if (TitleRadioButton.IsChecked == true)
            {
                SearchField = "Title";
            }
            if (AuthorRadioButton.IsChecked == true)
            {
                SearchField = "Author";
            }
            if (SubjectRadioButton.IsChecked == true)
            {
                SearchField = "Subject1 LIKE " + SearchTerm + " OR Subject2 LIKE " + SearchTerm + " OR Subject3 LIKE " + SearchTerm + " OR Subject4 LIKE " + SearchTerm + " OR Subject5";
            }
            if (ISBNRadioButton.IsChecked == true)
            {
                SearchField = "ISBN";
            }

            string SearchItemCommand = "SELECT * FROM ItemDatabase WHERE " + SearchField + " LIKE " + SearchTerm;
            SQLiteCommand SearchItemsql = new SQLiteCommand(SearchItemCommand, searchCatalog);

            SQLiteDataReader reader = SearchItemsql.ExecuteReader();

            List<ItemData> SearchResults = new List<ItemData>();
            while(reader.Read())
            {
                SearchResults.Add(new ItemData()
                {
                    ItemID = reader["ItemNumber"].ToString(),
                    Title = reader["Title"].ToString(),
                    Author = reader["Author"].ToString(),
                    Subject = reader["Subject1"].ToString(),
                    CallNo = reader["Classification"].ToString() + " " + reader["CallLetters"].ToString(),
                    CheckedOut = CheckedOutStatus(reader["CheckedOut"].ToString()),
                    DueDate = reader["DueDate"].ToString(),
                    CheckedOutTo = getPatron(reader["PatronID"].ToString())
                }); 
            }


            SearchResultsGrid.ItemsSource = SearchResults;

            this.InvalidateVisual();

            searchCatalog.Close();

        }

        private void ClearPatronDBButton_Click(object sender, RoutedEventArgs e)
        {
            SQLiteConnection dropPatronDB = new SQLiteConnection("Data Source=LibraryDatabase.sqlite;Version=3;datetimeformat=CurrentCulture");
            dropPatronDB.Open();

            //Delete Patron Database
            string dropPatronDBCommand = "DROP PatronDatabase";
            SQLiteCommand dropPatronDBsql = new SQLiteCommand(dropPatronDBCommand, dropPatronDB);
            dropPatronDBsql.ExecuteNonQuery();

            //Create Patron Database
            string PatronDBCommand = "CREATE TABLE PatronDatabase (PatronID INTEGER PRIMARY KEY AUTOINCREMENT, FirstName VARCHAR(80), LastName VARCHAR(100), StreetAddress VARCHAR(500), City VARCHAR(100), State VARCHAR(2), ZIP INTEGER(5), ChurchMember BOOLEAN, EmailAddress VARCHAR(200), PhoneNumber VARCHAR(10))";
            SQLiteCommand createPatronDB = new SQLiteCommand(PatronDBCommand, dropPatronDB);
            createPatronDB.ExecuteNonQuery();
            dropPatronDB.Close();

            MessageBox.Show("Patron Database cleared!");

        }

        private void CheckOutCatalogButton_Click(object sender, RoutedEventArgs e)
        {
            if (SearchResultsGrid.SelectedItem == null)
            {
                MessageBox.Show("No Item Selected!");
                return;
            }

            if (((ItemData)SearchResultsGrid.SelectedItem).CheckedOut == "True")
            {
                MessageBox.Show("Item is already checked out!");
                return;
            }
            //Get selected catalog item from DataGrid
            List<ItemData> CheckOutList = new List<ItemData>();

 

            SQLiteConnection addCheckOutItem = new SQLiteConnection("Data Source=LibraryDatabase.sqlite;Version=3;datetimeformat=CurrentCulture");
            addCheckOutItem.Open();

            
            string addItemToList = "INSERT INTO CheckOutList VALUES (\"" + ((ItemData)SearchResultsGrid.SelectedItem).ItemID + "\", \"" +
                ((ItemData)SearchResultsGrid.SelectedItem).Title + "\", \"" + ((ItemData)SearchResultsGrid.SelectedItem).Author + "\", \"" + ((ItemData)SearchResultsGrid.SelectedItem).Subject + "\", \"" + ((ItemData)SearchResultsGrid.SelectedItem).CallNo + "\")";

            SQLiteCommand addItemToListCommand = new SQLiteCommand(addItemToList, addCheckOutItem);
            addItemToListCommand.ExecuteNonQuery();

            string getCurrentList = "SELECT * FROM CheckOutList";
            SQLiteCommand getCurrentListCommand = new SQLiteCommand(getCurrentList, addCheckOutItem);

            SQLiteDataReader reader = getCurrentListCommand.ExecuteReader();

            while(reader.Read())
            {
                CheckOutList.Add(new ItemData()
                {
                    ItemID = reader["ItemID"].ToString(),
                    Title = reader["Title"].ToString(),
                    Author = reader["Author"].ToString(),
                    Subject = reader["Subject"].ToString(),
                    CallNo = reader["CallNo"].ToString(),
                });
            }

            CheckOutListDataGrid.ItemsSource = CheckOutList;

            addCheckOutItem.Close();

           //Calculate Checkout Date
            DateTime rightNow = DateTime.Now;
            DateTime checkOutDate = rightNow.AddDays(SettingsValues.DefaultDays);
            ItemDueDateOutPicker.Text = checkOutDate.ToString();
            SearchResultsGrid.ItemsSource = null;

              
            this.InvalidateVisual();
        }

        private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            //Open Setings database
            SQLiteConnection saveSettings = new SQLiteConnection("Data Source=LibraryDatabase.sqlite;Version=3;datetimeformat=CurrentCulture");
            saveSettings.Open();

            int FineOverride;

            if (AllowFineOverrideCheckbox.IsChecked == true)
            {
                FineOverride = 1;
            }
            else
            {
                FineOverride = 0;
            }

            //Build SQL command
            string settingsCommand = "UPDATE SettingsDatabase SET DefaultDays = " + DefaulDaysBox.Text + ", DefaultFine = " + DefaultFineBox.Text + ", MaxItems = " + MaxItemsBox.Text + ", AllowFineOverride = " + FineOverride + ", ChurchName = \"" + ChurchNameBox.Text + "\"";
            SQLiteCommand settingCom = new SQLiteCommand(settingsCommand, saveSettings);
            settingCom.ExecuteNonQuery();


            string settingLoadCommand = "SELECT * FROM SettingsDatabase";
            SQLiteCommand loadSettingsCommand = new SQLiteCommand(settingLoadCommand, saveSettings);

            SQLiteDataReader reader = loadSettingsCommand.ExecuteReader();
            // Apply settings
            SettingsValues.DefaultDays = Int32.Parse(reader["DefaultDays"].ToString());
            SettingsValues.DefaultFine = float.Parse(reader["DefaultFine"].ToString());
            SettingsValues.MaxItems = Int32.Parse(reader["MaxItems"].ToString());
            SettingsValues.AllowFineOverride = (reader["AllowFineOverride"] as int?) == 1;
            SettingsValues.ChurchName = reader["ChurchName"].ToString();

            MessageBox.Show("Settings saved and applied!");

        }

        private void PatronSearchOutButton_Click(object sender, RoutedEventArgs e)
        {
            //Function to compute number of items checked out
            int countItems(string ID)
            {
                SQLiteConnection countPatronItems = new SQLiteConnection("Data Source=LibraryDatabase.sqlite;Version=3;datetimeformat=CurrentCulture");
                countPatronItems.Open();

                string countCommand = "SELECT COUNT(*) FROM ItemDatabase WHERE PatronID = " + ID;
                SQLiteCommand count_command = new SQLiteCommand(countCommand, countPatronItems);

                return Convert.ToInt32(count_command.ExecuteScalar());
            }
            
            // Data validation
            if (PatronFirstNameOutBox.Text == "")
            {
                MessageBox.Show("First Name required!");
                return;
            }
            if (PatronLastNameOutBox.Text =="")
            {
                MessageBox.Show("Last Name required!");
                return;
            }

            //Open Database connection
            SQLiteConnection searchPatron = new SQLiteConnection("Data Source=LibraryDatabase.sqlite;Version=3;datetimeformat=CurrentCulture");
            searchPatron.Open();

            string FirstName = PatronFirstNameOutBox.Text.Trim();
            string LastName = PatronLastNameOutBox.Text.Trim();

            string patronSearchCommand = "SELECT * FROM PatronDatabase WHERE FirstName LIKE '%" + FirstName + "%' AND LastName LIKE '%" + LastName +"%'";
            SQLiteCommand patronSearch = new SQLiteCommand(patronSearchCommand, searchPatron);

            SQLiteDataReader reader = patronSearch.ExecuteReader();

            List<PatronData> SearchResults = new List<PatronData>();

            // Populate Data Grid of Patrons
            while (reader.Read())
            {
                SearchResults.Add(new PatronData()
                {
                    PatronID = reader["PatronID"].ToString(),
                    FirstName = reader["FirstName"].ToString(),
                    LastName = reader["LastName"].ToString(),
                    ItemsCheckedOut = countItems(reader["PatronID"].ToString()),
                    IsMember = (reader["ChurchMember"] as int?) == 1
                }) ;
            }

            searchPatron.Close();
            PatronOutDataGrid.ItemsSource = SearchResults;
            this.InvalidateVisual();

        }

        private void CheckOutButton_Click(object sender, RoutedEventArgs e)
        {

            if (PatronOutDataGrid.SelectedItem == null)
            {
                MessageBox.Show("No Patron Selected!");
                return;
            }
            CurrentID.CurrentPatronID = ((PatronData)PatronOutDataGrid.SelectedItem).PatronID;

            //Ensure they aren't over the max
            int ItemsCheckedOut = ((PatronData)PatronOutDataGrid.SelectedItem).ItemsCheckedOut;

            if (ItemsCheckedOut > SettingsValues.MaxItems)
            {
                MessageBox.Show("Patron has maximum items checked out!");
                return;
            }

            //Open SQL database
            SQLiteConnection checkOut = new SQLiteConnection("Data Source=LibraryDatabase.sqlite;Version=3;datetimeformat=CurrentCulture");
            checkOut.Open();

            int itemCount = 0;
            for (int i = 0; i < CheckOutListDataGrid.Items.Count; i++)
            {
                ItemData selectedItem = (ItemData)CheckOutListDataGrid.Items[i];
                string ItemID = Convert.ToString(selectedItem.ItemID);

                //Check Media Type
                string getMediaType = "SELECT MediaDesignation FROM ItemDatabase WHERE ItemNumber = " + ItemID;
                SQLiteCommand getAgeCodeCommand = new SQLiteCommand(getMediaType, checkOut);
                SQLiteDataReader reader = getAgeCodeCommand.ExecuteReader();
                string mediaType = reader["MediaDesignation"].ToString();

                string getPatronBirthdate = "SELECT BirthDate FROM PatronDatabase WHERE PatronID = " + CurrentID.CurrentPatronID;
                SQLiteCommand getPatronBDAYComamnd = new SQLiteCommand(getPatronBirthdate, checkOut);
                reader = getPatronBDAYComamnd.ExecuteReader();
                int patronAge = (DateTime.Now - DateTime.Parse(reader["BirthDate"].ToString())).Days / 365;

                if ((mediaType == "VH" | mediaType == "VD") & patronAge < 18)
                {
                    MessageBox.Show("Patron cannot check out " + selectedItem.Title + " because of age restrictions!");
                }
                else
                {
                    string checkInItemsCommand = "UPDATE ItemDatabase SET CheckedOut = 1, DueDate = \"" + ItemDueDateOutPicker.Text + "\", PatronID = \"" + CurrentID.CurrentPatronID + "\" WHERE ItemNumber = " + ItemID;
                    SQLiteCommand checkInItems = new SQLiteCommand(checkInItemsCommand, checkOut);
                    checkInItems.ExecuteNonQuery();

                    string checkOutHistoryAdd = "INSERT INTO HistoryDatabase VALUES (NULL, \"" + ItemID + "\", \"" + CurrentID.CurrentPatronID + "\", \"" + DateTime.Now.ToString() + "\", NULL)";
                    SQLiteCommand addHistoryCommand = new SQLiteCommand(checkOutHistoryAdd, checkOut);
                    addHistoryCommand.ExecuteNonQuery();

                    itemCount++;
                }

            }
            string dropCheckOutList = "DELETE FROM CheckOutList";
            SQLiteCommand dropCheckOutListCommand = new SQLiteCommand(dropCheckOutList, checkOut);
            dropCheckOutListCommand.ExecuteNonQuery();
            checkOut.Close();

            PatronCurrentItems.ItemsSource = null;

            MessageBox.Show(itemCount.ToString() + " items checked out!");

            //Clear data
            PatronOutDataGrid.ItemsSource = null;
            CheckOutListDataGrid.ItemsSource = null;

            CurrentID.CurrentItemID = "";

            PatronFirstNameOutBox.Text = "";
            PatronLastNameOutBox.Text = "";
            SearchCardCatalogBox.Text = "";

            this.InvalidateVisual();

        }

        private void CheckInCatalogButton_Click(object sender, RoutedEventArgs e)
        {
            if (SearchResultsGrid.SelectedItem == null)
            {
                MessageBox.Show("No Item Selected!");
                return;
            }
            if (((ItemData)SearchResultsGrid.SelectedItem).CheckedOut == "False")
            {
                MessageBox.Show("Item is already checked in!");
                return;
            }

            SearchResultsGrid.ItemsSource = null;


            //Switch to Check In Tab
            MainTabControl.SelectedIndex = 0;
            this.InvalidateVisual();
        }

     
        private void ItemSearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (SearchResultsGrid.SelectedItem == null)
            {
                MessageBox.Show("No Item Selected!");
                return;
            }
            //Get selected catalog item from DataGrid
            CurrentID.CurrentItemID = ((ItemData)SearchResultsGrid.SelectedItem).ItemID;

            //Open SQL connection
            SQLiteConnection itemSearch = new SQLiteConnection("Data Source=LibraryDatabase.sqlite;Version=3;datetimeformat=CurrentCulture");
            itemSearch.Open();

            string itemSearchCommand = "SELECT * FROM ItemDatabase WHERE ItemNumber = " + CurrentID.CurrentItemID;
            SQLiteCommand itemSearchsql = new SQLiteCommand(itemSearchCommand, itemSearch);
            SQLiteDataReader reader = itemSearchsql.ExecuteReader();

            //Clear all fields
            ItemTitleBox.Text = reader["Title"].ToString();
            ItemAuthorBox.Text = reader["Author"].ToString();
            ItemDateAcquiredPicker.Text = reader["DateAcquired"].ToString();
            ItemPriceBox.Text = reader["Price"].ToString();
            ItemClassBox.Text = reader["Classification"].ToString();
            ItemCallNoBox.Text = reader["CallLetters"].ToString();
            ItemCopyrightBox.Text = reader["Copyright"].ToString();
            ItemPublisherBox.Text = reader["Publisher"].ToString();
            ItemSourceBox.Text = reader["Source"].ToString();
            ItemExtentBox.Text = reader["Extent"].ToString();
            ItemRemarksBox.Text = reader["Remarks"].ToString();
            ItemISBNBox.Text = reader["ISBN"].ToString();
            ItemEditionBox.Text = reader["Edition"].ToString();
            ItemNotesBox.Text = reader["Notes"].ToString();
            ItemRateBox.Text = reader["Rate"].ToString();
            ItemDonorNameBox.Text = reader["DonorName"].ToString();
            ItemDonationDatePicker.Text = reader["DonationDate"].ToString();
            ItemInMemoryBox.Text = reader["InMemory"].ToString();
            ItemAgeCodeBox.Text = reader["AgeCode"].ToString();
            ItemMediaTypeBox.Text = reader["MediaDesignation"].ToString();
            ItemHonoreeName.Text = reader["HonoreeName"].ToString();
            ItemSubjectsBox.Text = reader["Subject1"].ToString() + ", " + reader["Subject2"].ToString() + ", " + reader["Subject3"].ToString() + ", " + reader["Subject4"].ToString() + ", " + reader["Subject5"].ToString();
            ItemDonorNotesBox.Text = reader["DonorText1"].ToString() + ", " + reader["DonorText2"].ToString();

            itemSearch.Close();
            //Switch to Item Tab
            MainTabControl.SelectedIndex = 2;
            this.InvalidateVisual();
        }

        private void ImportItemButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog FilePicker = new OpenFileDialog();

            FilePicker.InitialDirectory = @"C:\";
            FilePicker.Title = "Select Item Import File";
            FilePicker.DefaultExt = "csv";
            FilePicker.Filter = "CSV Files (*.csv)|*.csv";
            FilePicker.ShowDialog();

            string import_path = FilePicker.FileName;
            if (import_path == "")
            {
                return;
            }

            //Open File for reading
            StreamReader sr = new StreamReader(import_path);
            string line;
            string[] row;

            //Open Database connection
            SQLiteConnection addItem = new SQLiteConnection("Data Source=LibraryDatabase.sqlite;Version=3;datetimeformat=CurrentCulture");
            addItem.Open();

            int itemCount = 0;
            while ((line = sr.ReadLine()) != null)
            {   
                
                row = line.Replace("\"","").Split('\\');

                string Author = "";
                //Assign variables for SQL statement
                string Title = "\"" + row[1] + "\"";
                if (row[0] != "")
                {
                    string[] preAuthor = row[0].Split(',');
                    if (preAuthor.Length > 1)
                    {
                        Author = preAuthor[1] + " " + preAuthor[0];
                    }
                }
                string DateAcquired = row[2];
                string Price = row[3];
                string Class = row[4];
                string CallNo = row[5];
                string Copyright = row[6];
                string Publisher = row[7];
                string Source = row[8];
                string Extent = row[9];
                string Remarks = row[10];
                string ISBN = row[11];
                string Edition = row[12];
                string Notes = row[13];
                string Rate = row[19];
                string DonorName = row[20];
                string DonationDate = row[21];
                string InMemory = row[25];
                string AgeCode = row[19];
                string MediaType = row[27];
                string HonoreeName = row[23];
                string Subject1 = row[14];
                string Subject2 = row[15];
                string Subject3 = row[16];
                string Subject4 = row[17];
                string Subject5 = row[18];
                string DonorText1 = row[21];
                string DonorText2 = row[22];


                Author = Author.Replace("\"","");
                // Replace blank data with NULLs for SQL
                Author = "\"" + Author + "\"";

                if (DateAcquired == "")
                {
                    DateAcquired = "NULL";
                }
                else
                {
                    DateAcquired = "\"" + DateAcquired + "\"";
                }
                if (Price == "")
                {
                    Price = "NULL";
                }
                else
                {
                    Price = "\"" + Price + "\"";
                }
                if (Class == "")
                {
                    Class = "NULL";
                }
                else
                {
                    Class = "\"" + Class + "\"";
                }
                if (Copyright == "")
                {
                    Copyright = "NULL";
                }
                else
                {
                    Copyright = "\"" + Copyright + "\"";
                }
                if (Publisher == "")
                {
                    Publisher = "NULL";
                }
                else
                {
                    Publisher = "\"" + Publisher + "\"";
                }
                if (Source == "")
                {
                    Source = "NULL";
                }
                else
                {
                    Source = "\"" + Source + "\"";
                }
                if (Extent == "")
                {
                    Extent = "NULL";
                }
                else
                {
                    Extent = "\"" + Extent + "\"";
                }
                if (Remarks == "")
                {
                    Remarks = "NULL";
                }
                else
                {
                    Remarks = "\"" + Remarks + "\"";
                }
                if (ISBN == "")
                {
                    ISBN = "NULL";
                }
                else
                {
                    ISBN = "\"" + ISBN + "\"";
                }
                if (Edition == "")
                {
                    Edition = "NULL";
                }
                else
                {
                    Edition = "\"" + Edition + "\"";
                }
                if (Notes == "")
                {
                    Notes = "NULL";
                }
                else
                {
                    Notes = "\"" + Notes + "\"";
                }
                if (Rate == "")
                {
                    Rate = "NULL";
                }
                else
                {
                    Rate = "\"" + Rate + "\"";
                }
                if (DonorName == "")
                {
                    DonorName = "NULL";
                }
                else
                {
                    DonorName = "\"" + DonorName + "\"";
                }
                if (DonationDate == "")
                {
                    DonationDate = "NULL";
                }
                else
                {
                    DonationDate = "\"" + DonationDate + "\"";
                }
                if (InMemory == "")
                {
                    InMemory = "NULL";
                }
                else
                {
                    InMemory = "\"" + InMemory + "\"";
                }
                if (AgeCode == "")
                {
                    AgeCode = "NULL";
                }
                else
                {
                    AgeCode = "\"" + AgeCode + "\"";
                }
                if (MediaType == "")
                {
                    MediaType = "NULL";
                }
                else
                {
                    MediaType = "\"" + MediaType + "\"";
                }
                if (HonoreeName == "")
                {
                    HonoreeName = "NULL";
                }
                else
                {
                    HonoreeName = "\"" + HonoreeName + "\"";
                }
                if (Subject1 == "")
                {
                    Subject1 = "NULL";
                }
                else
                {
                    Subject1 = "\"" + Subject1 + "\"";
                }
                if (Subject2 == "")
                {
                    Subject2 = "NULL";
                }
                else
                {
                    Subject2 = "\"" + Subject2 + "\"";
                }
                if (Subject3 == "")
                {
                    Subject3 = "NULL";
                }
                else
                {
                    Subject3 = "\"" + Subject3 + "\"";
                }
                if (Subject4 == "")
                {
                    Subject4 = "NULL";
                }
                else
                {
                    Subject4 = "\"" + Subject4 + "\"";
                }
                if (Subject5 == "")
                {
                    Subject5 = "NULL";
                }
                else
                {
                    Subject5 = "\"" + Subject5 + "\"";
                }
                if (DonorText1 == "")
                {
                    DonorText1 = "NULL";
                }
                else
                {
                    DonorText1 = "\"" + DonorText1 + "\"";
                }
                if (DonorText2 == "")
                {
                    DonorText2 = "NULL";
                }
                else
                {
                    DonorText2 = "\"" + DonorText2 + "\"";
                }
                if (CallNo == "")
                {
                    CallNo = "NULL";
                }
                else
                {
                    CallNo = "\"" + CallNo + "\"";
                }

                //Build Database command
                string addItemCommand = "INSERT INTO ItemDatabase VALUES (NULL, " + Author + ", " + Title + ", " + DateAcquired + ", " + Price + ", " + Class + ", " + CallNo + ", " + Copyright + ", " + Publisher + ", " +
                    Source + ", " + Extent + ", " + Remarks + ", " + ISBN + ", " + Edition + ", " + Notes + ", " + Subject1 + ", " + Subject2 + ", " + Subject3 + ", " + Subject4 + ", " + Subject5 + ", " +
                    Rate + ", " + DonorName + ", " + DonorText1 + ", " + DonorText2 + ", " + DonationDate + ", " + InMemory + ", " + AgeCode + ", " + MediaType + ", " + HonoreeName + ", 0, NULL, NULL)";

                SQLiteCommand addItemToDB = new SQLiteCommand(addItemCommand, addItem);
                addItemToDB.ExecuteNonQuery();
                itemCount++;
            }
            sr.Close();
            MessageBox.Show(itemCount.ToString() + " items imported!");
            addItem.Close();
        }

        private void ViewPatronDBButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog FilePicker = new OpenFileDialog();

            FilePicker.InitialDirectory = @"C:\";
            FilePicker.Title = "Select Item Import File";
            FilePicker.DefaultExt = "csv";
            FilePicker.Filter = "CSV Files (*.csv)|*.csv";
            FilePicker.ShowDialog();

            string import_path = FilePicker.FileName;

            if (import_path == "")
            {
                return;
            }

            //Open File for reading
            StreamReader sr = new StreamReader(import_path);

            //Open Database connection
            SQLiteConnection addPatron = new SQLiteConnection("Data Source=LibraryDatabase.sqlite;Version=3;datetimeformat=CurrentCulture");
            addPatron.Open();

            string line;
            string[] row;

            
            int patronCount = 0;
            while ((line = sr.ReadLine()) != null)
            {
                row = line.Split(',');
                //Assign text box values to local variables
                string FirstName = row[0];
                string LastName = row[1];
                string Address = row[2];
                string City = row[3];
                string State = row[4];
                string ZIP = row[5];
                string ChurchMember = row[6];
                string Email = row[7];
                string Phone = row[8];
                string BirthDate = row[9];

                 //Validate data
                if (FirstName == "")
                {
                    MessageBox.Show("First Name required!");
                    return;
                }
                else
                {
                    FirstName = "\"" + FirstName + "\"";
                }
                if (LastName == "")
                {
                    MessageBox.Show("Last Name required!");
                    return;
                }
                else
                {
                    LastName = "\"" + LastName + "\"";
                }
                if (Address == "")
                {
                    MessageBox.Show("Street address required!");
                    return;
                }
                else
                {
                    Address = "\"" + Address + "\"";
                }
                if (City == "")
                {
                    MessageBox.Show("City required!");
                    return;
                }
                else
                {
                    City = "\"" + City + "\"";
                }
                if (State == "")
                {
                    MessageBox.Show("State required!");
                    return;
                }
                else
                {
                    State = "\"" + State + "\"";
                }
                if (ZIP == "")
                {
                    MessageBox.Show("ZIP Code required!");
                    return;
                }
                else
                {
                    ZIP = "\"" + ZIP + "\"";
                }

                if (Email == "")
                {
                    Email = "NULL";
                }
                else
                {
                    Email = "\"" + Email + "\"";
                }
                if (Phone == "")
                {
                    Phone = "NULL";
                }
                else
                {
                    Phone = "\"" + Phone + "\"";
                }
                if (BirthDate == "")
                {
                    BirthDate = "NULL";
                }
                else
                {
                    BirthDate = "\"" + BirthDate + "\"";
                }
                if (ChurchMember == "")
                {
                    ChurchMember = "NULL";
                }
                else
                {
                    ChurchMember = "\"" + ChurchMember + "\"";
                }
                string addPatronCommand = "INSERT INTO PatronDatabase VALUES (NULL, " + FirstName + ", " + LastName + ", " + Address + ", " + City + ", " + State + ", " + ZIP + ", " + ChurchMember + ", " + Email + ", " + Phone + ", " + BirthDate + ")";
                SQLiteCommand addPatronToDB = new SQLiteCommand(addPatronCommand, addPatron);
                addPatronToDB.ExecuteNonQuery();
                patronCount++;

            }
            addPatron.Close();
        }

        private void PatronCurrentItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void ClearItemDBButton_Click(object sender, RoutedEventArgs e)
        {
            SQLiteConnection clearItemDB = new SQLiteConnection("Data Source=LibraryDatabase.sqlite;Version=3;datetimeformat=CurrentCulture");
            clearItemDB.Open();

            // Drop Item Database
            string dropItems = "DROP TABLE ItemDatabase";

            SQLiteCommand dropItemCommand = new SQLiteCommand(dropItems, clearItemDB);
            dropItemCommand.ExecuteNonQuery();

            //Create the item database
            string ItemDBCommand = "CREATE TABLE ItemDatabase (ItemNumber INTEGER PRIMARY KEY AUTOINCREMENT, Author VARCHAR(100), Title VARCHAR(400), DateAcquired DATE, Price DECIMAL(3,2), Classification VARCHAR(10), CallLetters VARCHAR(3), Copyright INTEGER, " +
                "Publisher VARCHAR(60), Source VARCHAR(10), Extent VARCHAR(30), Remarks VARCHAR(500), ISBN VARCHAR(25), Edition VARCHAR(40), Notes VARCHAR(500), Subject1 VARCHAR(50), Subject2 VARCHAR(50), " + "" +
                "Subject3 VARCHAR(50), Subject4 VARCHAR(50), Subject5 VARCHAR(50), Rate VARCHAR(1), DonorName VARCHAR(80), DonorText1 VARCHAR(80), DonorText2 VARCHAR(80), " +
                "DonationDate DATE, InMemory VARCHAR(1), AgeCode VARCHAR(1), MediaDesignation VARCHAR(2), HonoreeName VARCHAR(100), CheckedOut BOOLEAN, DueDate DATE, PatronID INTEGER)";
            SQLiteCommand createItemDB = new SQLiteCommand(ItemDBCommand, clearItemDB);
            createItemDB.ExecuteNonQuery();

            MessageBox.Show("Item database cleared!");
            clearItemDB.Close();
        }

        private void ViewCheckedOutButton_Click(object sender, RoutedEventArgs e)
        {
            string CheckedOutStatus(string CheckedOut)
            {
                if (CheckedOut == "True")
                {
                    return "Yes";
                }
                else
                {
                    return "No";
                }
            }

            //Function to get name of patron with item checked out
            string getPatron (string patronID)
            {
                if (patronID == "")
                {
                    return "";
                }
                SQLiteConnection getPatronName = new SQLiteConnection("Data Source=LibraryDatabase.sqlite;Version=3;datetimeformat=CurrentCulture");
                getPatronName.Open();

                string getPatronNameCommand = "SELECT FirstName, LastName FROM PatronDatabase WHERE PatronID = " + patronID;
                SQLiteCommand patronIDsql = new SQLiteCommand(getPatronNameCommand, getPatronName);
                SQLiteDataReader name_reader = patronIDsql.ExecuteReader();

                return name_reader["FirstName"].ToString() + " " + name_reader["LastName"].ToString();

            }

            SQLiteConnection getCheckedOutItems = new SQLiteConnection("Data Source=LibraryDatabase.sqlite;Version=3;datetimeformat=CurrentCulture");
            getCheckedOutItems.Open();

            string getItemsSql = "SELECT * FROM ItemDatabase WHERE CheckedOut = 1";

            SQLiteCommand getItemCommand = new SQLiteCommand(getItemsSql, getCheckedOutItems);

            SQLiteDataReader reader = getItemCommand.ExecuteReader();

            List<ItemData> SearchResults = new List<ItemData>();
            while (reader.Read())
            {
                SearchResults.Add(new ItemData()
                {
                    ItemID = reader["ItemNumber"].ToString(),
                    Title = reader["Title"].ToString(),
                    Author = reader["Author"].ToString(),
                    Subject = reader["Subject1"].ToString(),
                    CallNo = reader["Classification"].ToString() + " " + reader["CallLetters"].ToString(),
                    CheckedOut = CheckedOutStatus(reader["CheckedOut"].ToString()),
                    DueDate = reader["DueDate"].ToString(),
                    CheckedOutTo = getPatron(reader["PatronID"].ToString())
                });
            }


            ReportGrid.ItemsSource = SearchResults;

            this.InvalidateVisual();

            getCheckedOutItems.Close();


        }

        private void BackupDatabaseButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog FilePicker = new OpenFileDialog();

            FilePicker.InitialDirectory = @"C:\";
            FilePicker.Title = "Select Backup File";
            FilePicker.DefaultExt = "sqlite";
            FilePicker.Filter = "Database Files (*.sqlite)|*.sqlite";
            FilePicker.CheckFileExists = false;
            FilePicker.ShowDialog();

            string backup_path = FilePicker.FileName;

            if (backup_path == "")
            {
                return;
            }

            File.Copy("LibraryDatabase.sqlite", backup_path);

            MessageBox.Show("Backup Complete!");


        }

        private void CheckInButton_Click(object sender, RoutedEventArgs e)
        {
            SQLiteConnection checkInSelectedItems = new SQLiteConnection("Data Source=LibraryDatabase.sqlite;Version=3;datetimeformat=CurrentCulture");
            checkInSelectedItems.Open();
            int itemCount = 0;
            float TotalFine = 0;

            for (int i = 0; i < PatronCurrentItems.SelectedItems.Count; i++)
            {
                float CurrentItemFine = 0;

                ItemData selectedItem = (ItemData)PatronCurrentItems.SelectedItems[i];
                int dayDifference = Convert.ToInt32((DateTime.Now - DateTime.Parse(selectedItem.DueDate)).TotalDays);

                if (dayDifference > 0)
                {
                    CurrentItemFine = (float)dayDifference * SettingsValues.DefaultFine;
                }
                TotalFine = CurrentItemFine + TotalFine;
                string ItemID = Convert.ToString(selectedItem.ItemID);
                string checkInItemsCommand = "UPDATE ItemDatabase SET CheckedOut = 0, DueDate = NULL, PatronID = NULL WHERE ItemNumber = " + ItemID;
                SQLiteCommand checkInItems = new SQLiteCommand(checkInItemsCommand, checkInSelectedItems);
                checkInItems.ExecuteNonQuery();

                string checkInHistoryCommand = "UPDATE HistoryDatabase SET CheckInDate = \"" + DateTime.Now.ToString() + "\" WHERE PatronID = " + CurrentID.CurrentPatronID + " AND ItemID = " + ItemID;
                SQLiteCommand checkInHistory = new SQLiteCommand(checkInHistoryCommand, checkInSelectedItems);
                checkInHistory.ExecuteNonQuery();

                itemCount++;

                
            }

            string getCurrentFine = "SELECT FinesOwed FROM PatronDatabase WHERE PatronID = " + CurrentID.CurrentPatronID;
            SQLiteCommand getCurrentFineCommand = new SQLiteCommand(getCurrentFine, checkInSelectedItems);
            SQLiteDataReader reader = getCurrentFineCommand.ExecuteReader();

            float currentPatronFine = float.Parse(reader["FinesOwed"].ToString());
            float newFine = currentPatronFine + TotalFine;

            string addNewFine = "UPDATE PatronDatabase SET FinesOwed = " + newFine.ToString("0.00") + " WHERE PatronID = " + CurrentID.CurrentPatronID;
            SQLiteCommand addNewFineCommand = new SQLiteCommand(addNewFine, checkInSelectedItems);
            addNewFineCommand.ExecuteNonQuery();

            checkInSelectedItems.Close();
            PatronCurrentItems.ItemsSource = null;
            PatronInDataGrid.ItemsSource = null;
            PatronFirstNameInBox.Text = "";
            PatronLastNameInBox.Text = "";
            CurrentID.CurrentPatronID = "";
            this.InvalidateVisual();
            MessageBox.Show(itemCount.ToString() + " items checked in!\nFines owed for this checkin: $" + TotalFine.ToString("0.00"));
        }

        private void ChangeTab(object sender, RoutedEventArgs e)
        {
            if (MainTabControl.SelectedIndex == 1)
            {
                this.InvalidateVisual();
            }
        }

        private void PatronSearchInButton_Click(object sender, RoutedEventArgs e)
        {
            //Function to compute number of items checked out
            int countItems(string ID)
            {
                SQLiteConnection countPatronItems = new SQLiteConnection("Data Source=LibraryDatabase.sqlite;Version=3;datetimeformat=CurrentCulture");
                countPatronItems.Open();

                string countCommand = "SELECT COUNT(*) FROM ItemDatabase WHERE PatronID = " + ID;
                SQLiteCommand count_command = new SQLiteCommand(countCommand, countPatronItems);

                return Convert.ToInt32(count_command.ExecuteScalar());
            }

            // Data validation
            if (PatronFirstNameInBox.Text == "")
            {
                MessageBox.Show("First Name required!");
                return;
            }
            if (PatronLastNameInBox.Text == "")
            {
                MessageBox.Show("Last Name required!");
                return;
            }

            //Open Database connection
            SQLiteConnection searchPatron = new SQLiteConnection("Data Source=LibraryDatabase.sqlite;Version=3;datetimeformat=CurrentCulture");
            searchPatron.Open();

            string FirstName = PatronFirstNameInBox.Text.Trim();
            string LastName = PatronLastNameInBox.Text.Trim();

            string patronSearchCommand = "SELECT * FROM PatronDatabase WHERE FirstName LIKE '%" + FirstName + "%' AND LastName LIKE '%" + LastName + "%'";
            SQLiteCommand patronSearch = new SQLiteCommand(patronSearchCommand, searchPatron);

            SQLiteDataReader reader = patronSearch.ExecuteReader();

            List<PatronData> SearchResults = new List<PatronData>();

            // Populate Data Grid of Patrons
            while (reader.Read())
            {
                SearchResults.Add(new PatronData()
                {
                    PatronID = reader["PatronID"].ToString(),
                    FirstName = reader["FirstName"].ToString(),
                    LastName = reader["LastName"].ToString(),
                    ItemsCheckedOut = countItems(reader["PatronID"].ToString()),
                    IsMember = (reader["ChurchMember"] as int?) == 1
                });
            }

            searchPatron.Close();
            PatronInDataGrid.ItemsSource = SearchResults;
            this.InvalidateVisual();
        }

        private void PatronIn_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string CheckedOutStatus(string DueDate)
            {
                if (DueDate == "True")
                {
                    return "Yes";
                }
                else
                {
                    return "No";
                }
            }
            if (PatronInDataGrid.SelectedItem == null)
            {
                return;
            }

            SQLiteConnection searchPatron = new SQLiteConnection("Data Source=LibraryDatabase.sqlite;Version=3;datetimeformat=CurrentCulture");
            searchPatron.Open();

            CurrentID.CurrentPatronID = ((PatronData)PatronInDataGrid.SelectedItem).PatronID;
            string itemSearchCommand = "SELECT * FROM ItemDatabase WHERE PatronID = " + CurrentID.CurrentPatronID;
            SQLiteCommand itemSearch = new SQLiteCommand(itemSearchCommand, searchPatron);
            SQLiteDataReader reader_item = itemSearch.ExecuteReader();

            List<ItemData> SearchResults = new List<ItemData>();
            while (reader_item.Read())
            {
                SearchResults.Add(new ItemData()
                {
                    ItemID = reader_item["ItemNumber"].ToString(),
                    Title = reader_item["Title"].ToString(),
                    Author = reader_item["Author"].ToString(),
                    Subject = reader_item["Subject1"].ToString(),
                    CallNo = reader_item["Classification"].ToString() + " " + reader_item["CallLetters"].ToString(),
                    CheckedOut = CheckedOutStatus(reader_item["CheckedOut"].ToString()),
                    DueDate = reader_item["DueDate"].ToString()
                });
            }

            PatronCurrentItems.ItemsSource = SearchResults;

            this.InvalidateVisual();

            searchPatron.Close();
        }

        private void CheckOutClearForm_Click(object sender, RoutedEventArgs e)
        {
            CheckOutListDataGrid.ItemsSource = null;
            PatronFirstNameOutBox.Text = "";
            PatronLastNameOutBox.Text = "";
            PatronOutDataGrid.ItemsSource = null;
            this.InvalidateVisual();

        }

        private void ClearCheckInButton_Click(object sender, RoutedEventArgs e)
        {
            PatronCurrentItems.ItemsSource = null;
            PatronFirstNameInBox.Text = "";
            PatronLastNameInBox.Text = "";
            PatronInDataGrid.ItemsSource = null;
            this.InvalidateVisual();
        }

        private void WindowClose(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SQLiteConnection closing = new SQLiteConnection("Data Source=LibraryDatabase.sqlite;Version=3;datetimeformat=CurrentCulture");
            closing.Open();

            string dropCheckOutList = "DELETE FROM CheckOutList";
            SQLiteCommand dropCheckOutListCommand = new SQLiteCommand(dropCheckOutList, closing);
            dropCheckOutListCommand.ExecuteNonQuery();
            closing.Close();
        }

        private void DisableDBControl(object sender, RoutedEventArgs e)
        {
            ClearItemDBButton.IsEnabled = false;
            ClearPatronDBButton.IsEnabled = false;
            ImportItemButton.IsEnabled = false;
            ViewPatronDBButton.IsEnabled = false;
            ClearPatronDBButton.IsEnabled = false;
            ClearItemDBButton.IsEnabled = false;
            InitializeDBButton.IsEnabled = false;
            this.InvalidateVisual();
            
        }

        private void EnableDBControl(object sender, RoutedEventArgs e)
        {
            ClearItemDBButton.IsEnabled = true;
            ClearPatronDBButton.IsEnabled = true;
            ImportItemButton.IsEnabled = true;
            ViewPatronDBButton.IsEnabled = true;
            ClearPatronDBButton.IsEnabled = true;
            ClearItemDBButton.IsEnabled = true;
            InitializeDBButton.IsEnabled = true;
            this.InvalidateVisual();

        }

        private void ViewHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            string getItemTitle(string ItemID)
            {
                SQLiteConnection getItem = new SQLiteConnection("Data Source=LibraryDatabase.sqlite;Version=3;datetimeformat=CurrentCulture");
                getItem.Open();

                string getItemCommand = "SELECT Title FROM ItemDatabase WHERE ItemNumber = " + ItemID;
                SQLiteCommand getItemTitleCommand = new SQLiteCommand(getItemCommand, getItem);
                SQLiteDataReader item_reader = getItemTitleCommand.ExecuteReader();
                return item_reader["Title"].ToString();
            }
            string getPatronName(string PatronID)
            {
                SQLiteConnection getPatron = new SQLiteConnection("Data Source=LibraryDatabase.sqlite;Version=3;datetimeformat=CurrentCulture");
                getPatron.Open();

                string getPatronCommand = "SELECT FirstName, LastName FROM PatronDatabase WHERE PatronID = " + PatronID;
                SQLiteCommand getPatronNameCommand = new SQLiteCommand(getPatronCommand, getPatron);
                SQLiteDataReader patron_reader = getPatronNameCommand.ExecuteReader();
                return patron_reader["FirstName"].ToString() + " " + patron_reader["LastName"].ToString();
            }

            SQLiteConnection historyData = new SQLiteConnection("Data Source=LibraryDatabase.sqlite;Version=3;datetimeformat=CurrentCulture");
            historyData.Open();

            string getHistoryData = "SELECT * FROM HistoryDatabase";
            SQLiteCommand getHistoryCommand = new SQLiteCommand(getHistoryData, historyData);
            SQLiteDataReader reader = getHistoryCommand.ExecuteReader();
            List<HistoryData> HistoryView = new List<HistoryData>();

            while (reader.Read())
            {
                HistoryView.Add(new HistoryData()
                {
                    TransactionID = reader["TransactionID"].ToString(),
                    ItemTitle = getItemTitle(reader["ItemID"].ToString()),
                    PatronName = getPatronName(reader["PatronID"].ToString()),
                    CheckOutDate = reader["CheckOutDate"].ToString(),
                    CheckInDate = reader["CheckInDate"].ToString()
                });
            }
            ReportGrid.ItemsSource = HistoryView;
            this.InvalidateVisual();

        }

        private void PayFineButton_Click(object sender, RoutedEventArgs e)
        {
            SQLiteConnection getFine = new SQLiteConnection("Data Source=LibraryDatabase.sqlite;Version=3;datetimeformat=CurrentCulture");
            getFine.Open();

            string getCurrentFine = "SELECT FinesOwed FROM PatronDatabase WHERE PatronID = " + CurrentID.CurrentPatronID;
            SQLiteCommand getCurrentFineCommand = new SQLiteCommand(getCurrentFine, getFine);
            SQLiteDataReader reader = getCurrentFineCommand.ExecuteReader();

            float currentFine = float.Parse(reader["FinesOwed"].ToString());

            if (currentFine <= 0)
            {
                MessageBox.Show("No fines owed!");
                return;
            }

            float Payment = float.Parse(FinePaymentBox.Text);

            if (Payment <= 0)
            {
                MessageBox.Show("Fine not valid or not formatted correctly!");
                return;
            }
            if (Payment > currentFine)
            {
                MessageBox.Show("Please specify an amount lower than " + currentFine.ToString("0.00"));
                return;
            }

            float newAmountOwed = currentFine - Payment;

            string applyPayment = "UPDATE PatronDatabase SET FinesOwed = \"" + newAmountOwed.ToString("0.00") + "\" WHERE PatronID = " + CurrentID.CurrentPatronID;
            SQLiteCommand applyPaymentCommand = new SQLiteCommand(applyPayment, getFine);
            applyPaymentCommand.ExecuteNonQuery();

            FinesOwedLabel.Content = "Fines Owed: $" + newAmountOwed.ToString("0.00");

            getFine.Close();

            FinePaymentBox.Text = "";

            MessageBox.Show("Payment applied!");
            this.InvalidateVisual();


        }

        private void ClearPatronFormButton_Click(object sender, RoutedEventArgs e)
        {
            PatronFirstNameBox.Text = "";
            PatronLastNameBox.Text = "";
            PatronAddressBox.Text = "";
            PatronCityBox.Text = "";
            PatronStateBox.Text = "";
            PatronZIPBox.Text = "";
            PatronMemberCheckbox.IsChecked = false;
            PatronEmailBox.Text = "";
            PatronPhoneBox.Text = "";
            BirthDateDatePicker.Text = "";
            FinePaymentBox.Text = "";
            FinesOwedLabel.Content = "Fines Owed: ";
            CurrentID.CurrentPatronID = "";

            this.InvalidateVisual();
        }
    }
        
}

