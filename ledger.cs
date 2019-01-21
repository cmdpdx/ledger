using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace Ledger {
    // A single user account to represent simple deposit/withdraw transactions.
    [Serializable()]
    public class Account {

        // Encapsulate storaging, hashing, and checking of passwords.
        [Serializable()]
        private class Password {
            private string hashedPassword;

            public Password(string password) {
                hashedPassword = GetPasswordHash(password);
            } 

            // Tests if hash of the supplied password matches the stored password hash. 
            public bool CheckPassword(string testPassword) {
                string testHash = GetPasswordHash(testPassword);
                StringComparer comparer = StringComparer.OrdinalIgnoreCase;
                return comparer.Compare(testHash, hashedPassword) == 0;
            }

            // Creates a new password hash; returns a string represeting the byte array
            private string GetPasswordHash(string input) {
                var sb = new StringBuilder();
                using (SHA256 sha256 = SHA256.Create()) {
                    byte[] data = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                    foreach (var b in data) {
                        sb.Append(b.ToString("x2"));
                    }
                }
                return sb.ToString();
            }
        }

        // Information stored for each transaction 
        [Serializable()]
        private struct Transaction {
            public DateTime TimeStamp { get; }
            public string Verb {get; set; }
            public double Value {get; set; }
            public double Balance {get; set; }

            public Transaction(string verb, double value, double balance) {
                TimeStamp = DateTime.Now;
                Verb = verb;
                Value = value;
                Balance = balance;
            }

            public override string ToString() {
                return $"{TimeStamp} : {Verb} (${Value}) : Balance ${Balance}";
            }
        }

        // Read-only Properties
        public string UserName { get; }
        public double Balance { get { return balance; } }
        public DateTime LastLogOn { get; private set; }
        public List<string> TransactionHistory {
            get {
                var output = new List<string>();
                foreach (var t in transactionHistory) {
                    output.Add(t.ToString());
                }
                return output;            
            }
        }

        // Non-property user account private fields
        private Password userPassword;
        private double balance;
        private List<Transaction> transactionHistory;

        public Account(string name, string pass) {
            UserName = name;
            userPassword = new Password(pass);
            balance = 0;
            LastLogOn = DateTime.Now;
            transactionHistory = new List<Transaction>();
        }

        // Attempt to deposit the given amount into the account balance.
        // Record successful deposits in the transaction history.
        // Returns: true/false on success/failure of deposit. 
        // Output parameter msg contains informational message on success/failure. 
        public bool Deposit(double amount, out string msg) {
            // Don't allow negative deposits
            if (amount < 0) {
                msg = $"Deposit amount ({amount}) must be positive.";
                return false; 
            }
            balance += amount;
            msg = $"Deposited ${amount}. Current balance ${balance}.";
            recordTransaction("deposit", amount);
            return true;
        }

        // Attempt to withdraw the given amount into the account balance.
        // Record successful withdrawls in the transaction history.
        // Returns: true/false on success/failure of withdrawl. 
        // Output parameter msg contains informational message on success/failure.
        public bool Withdraw(double amount, out string msg) {
            // Don't allow negative withdrawls
            if (amount < 0) {
                msg = $"Withdrawl amount ({amount}) must be positive.";
                return false;
            }
            balance -= amount;
            msg = $"Withdrew ${amount}. Current balance ${balance}.";
            recordTransaction("withdrawl", amount);
            return true;
        }

        public bool CheckPassword(string password) {
            return userPassword.CheckPassword(password);
        }

        public void UpdateLastLogOn() {
            LastLogOn = DateTime.Now;
        }

        // Record transaction; must include at least a verb and value.
        private void recordTransaction(string verb, double value) {
            transactionHistory.Add(new Transaction(verb, value, Balance));
        }
    }


    // Handles tracking multiple accounts: creating, logging on/off, account interaction,
    // and saving/loading lists of accounts. Prefered way to interact with Accounts. 
    public class AccountManager {
        private Account currentAccount;
        private List<Account> accounts;

        public AccountManager() {
            accounts = new List<Account>();
            currentAccount = null;
        }

        // Public properties; all read-only
        public bool LoggedOn {
            get { return currentAccount != null; }
        }
        public string UserName {
            get { return LoggedOn ? currentAccount.UserName : string.Empty; }
        }
        public double Balance {
            get { return LoggedOn ? currentAccount.Balance : 0; }
        }
        public List<string> TransactionHistory {
            get { return LoggedOn ? currentAccount.TransactionHistory : null; }
        }

        // Create a new user account with the supplied credentials.
        // Parameters:
        //  string user/pass - username and password, respectively, of account to create.
        //  string msg - Output parameter; message indicates success or failure (username already exists)
        // Returns: 
        //  bool - true if account successfully created; false otherwise
        public bool CreateAccount(string user, string pass, out string msg) {
            // Does this username already exist?
            if (isValidUser(user)) {
                msg = $"Username {user} already exists.";
                return false;
            }
            msg = $"Account created: {user}";
            currentAccount = new Account(user, pass);
            accounts.Add(currentAccount);
            return true;
        }

        // Attempt to log onto on account with the supplied credentials.
        // Parameters:
        //  string user/pass - username and password, respectively, of account.
        //  string msg - Output parameter; error message on failure, timestamp of last logon on success
        // Returns: 
        //  bool - true if successfully logged on; false otherwise
        public bool LogOn(string user, string pass, out string msg) {
            // Check that the user actually exists.
            if (!isValidUser(user)) {
                msg = $"Username {user} not recognized.";
                return false;
            }
            // Find the correct account, check the password supplied matches.
            var account = accounts.Find(x => x.UserName.Equals(user));
            if (!account.CheckPassword(pass)) {
                msg = "Invalid password.";
                return false;
            }
            // Set the currentAccount to the authenticated account and update last log on time
            currentAccount = account;
            msg = currentAccount.LastLogOn.ToString();
            currentAccount.UpdateLastLogOn();
            return true;
        }

        // Log out of the current account.
        public void LogOut() {
            //currentAccount.store();
            currentAccount = null;
        }

        // Attempt to deposit into the current account, if logged on.
        // Parameters:
        //  double amount - Value to deposit into the current account.
        //  string msg - Output parameter; error message on failure; confirmation on success
        // Returns:
        //  bool - true if successfully deposited; false otherwise.
        public bool Deposit(double amount, out string msg) {
            if (!LoggedOn) {
                msg = "Not logged in.";
                return false;
            }
            return currentAccount.Deposit(amount, out msg);
        }

        // Attempt to withdraw from the current account, if logged on.
        // Parameters:
        //  double amount - Value to withdraw from the current account.
        //  string msg - Output parameter; error message on failure; confirmation on success
        // Returns:
        //  bool - true if successfully withdrawn; false otherwise.
        public bool Withdraw(double amount, out string msg) {
            if (!LoggedOn) {
                msg = "Not logged in.";
                return false;
            }
            return currentAccount.Withdraw(amount, out msg);
        }

        // Load account information from the provided file.
        // Returns: true if succesfully loaded accounts; false otherwise.
        public bool LoadAccounts(string fileName, out string msg) { 
            if (!File.Exists(fileName)) {
                msg = $"File \"{fileName}\" not found.";
                return false;
            }

            bool success = true;
            List<Account> loadedAccounts = new List<Account>();
            Stream openFileStream = null;
            // Attempt to open the provided file and deserialize its contents. 
            try {
                openFileStream = File.OpenRead(fileName);
                BinaryFormatter deserializer = new BinaryFormatter();
                loadedAccounts = (List<Account>)deserializer.Deserialize(openFileStream);
                msg = $"Accounts loaded successfully from \"{fileName}\".";
            }
            catch (SerializationException e) {
                msg = "Error while deserializing: " + e.Message;
                success = false;
            }
            // File.OpenRead can throw many IO exceptions, catch them all here.
            catch (Exception e) {
                msg = "Error while opening the file: " + e.Message;
                success = false;
            }
            finally {
                if (openFileStream != null) 
                    openFileStream.Close();
            }

            // Clear the current account information and save reference to the loaded accounts
            if (success) {
                currentAccount = null;
                accounts.Clear();
                accounts = loadedAccounts;
            }
            return success;
        }

        // Save account information to the provided file.
        // Returns: true if succesfully saved accounts; false otherwise.
        public bool SaveAccounts(string fileName, out string msg){
            if (accounts.Count == 0) {
                msg = "No accounts to save.";
                return false;
            }

            bool success = true;
            Stream saveFileStream = null;
            // Attempt to create/overwrite the provided file and serialize the List of Accounts  
            try {
                saveFileStream = File.Create(fileName);
                BinaryFormatter serializer = new BinaryFormatter();
                serializer.Serialize(saveFileStream, accounts);
                msg = $"Account saved successfully to \"{fileName}\".";
            }
            catch (SerializationException e) {
                msg = "Error while serializing: " + e.Message;
                success = false;
            }
            // File.Create can throw many IO exceptions, catch them all here.
            catch (Exception e) {
                msg = "Error duing file creation/overwriting: "  + e.Message;
                success = false;
            }
            finally {
                if (saveFileStream != null) 
                    saveFileStream.Close();
            }
            
            return success;
        }

        // Private method: checks whether a given name matches in the list of 
        // current users known. Returns true if user is found, false otherwise.
        private bool isValidUser(string user) {
            foreach (var account in accounts) {
                if (account.UserName.Equals(user))
                    return true;
            }
            return false;
        }
    }
}
