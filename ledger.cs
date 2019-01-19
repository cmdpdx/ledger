using System;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Ledger {
    class Account {

        private class Password {
            private string hashedPassword;
            public Password(string password) {
                hashedPassword = GetPasswordHash(password);
            } 

            public bool CheckPassword(string testPassword) {
                string testHash = GetPasswordHash(testPassword);
                StringComparer comparer = StringComparer.OrdinalIgnoreCase;
                return comparer.Compare(testHash, hashedPassword) == 0;
            }

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

        private struct Transaction {
            public DateTime TimeStamp { get; }
            public string Verb {get; set; }
            public double Value {get; set; }
            public bool Success {get; set; }
            public string Message {get; set; }

            public Transaction(string verb, double value) {
                TimeStamp = DateTime.Now;
                Verb = verb;
                Value = value;
                Success = true;
                Message = string.Empty;
            }

            public Transaction(string verb, double value, bool success, string message) {
                TimeStamp = DateTime.Now;
                Verb = verb;
                Value = value;
                Success = success;
                Message = message;
            }

            public override string ToString() {
                string s = $"{TimeStamp} -- {Verb} (${Value}) : {Success ? "OK" : "FAILED"}";
                if (!Message.Equals(string.Empty)) {
                    s += $" : {Message}";
                }
                return s;
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

        // Non-property user account fields
        private Password userPassword;
        private double balance;
        private List<Transaction> transactionHistory;

        // Constructor
        public Account(string name, string pass) {
            UserName = name;
            userPassword = new Password(pass);
            balance = 0;
            LastLogOn = DateTime.Now;
            transactionHistory = new List<Transaction>();
        }

        // public methods
        public bool Deposit(double amount, out string msg) {
            if (amount < 0) {
                msg = $"Deposit amount ({amount}) must be positive.";
                return false; 
            }
            msg = $"Deposited {amount}.";
            balance += amount;
            recordTransaction("deposit", amount);
            return true;
        }

        public bool Withdraw(double amount, out string msg) {
            if (amount < 0) {
                msg = $"Withdrawl amount (${amount}) must be positive.";
                return false;
            }
            if (amount > balance) {
                recordTransaction("withdrawl", amount, false, "insufficient funds");
                msg = $"Insufficient funds to withdraw {amount}. (Current balance ${balance})";
                return false;
            }
            msg = $"Withdrew {amount}.";
            balance -= amount;
            recordTransaction("withdrawl", amount);
            return true;
        }

        public bool CheckPassword(string password) {
            return userPassword.CheckPassword(password);
        }

        public void UpdateLastLogOn() {
            LastLogOn = DateTime.Now;
        }

        // TODO: save account info to persistent storage
        public void store() {

        }

        // private methods
        private void recordTransaction(string verb, double value) {
            transactionHistory.Add(new Transaction(verb, value));
        }
        private void recordTransaction(string verb, double value, bool success, string message) {
            transactionHistory.Add(new Transaction(verb, value, success, message));
        }
    }


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
        //  string msg - Output variable; message indicates success or failure (username already exists)
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
        //  string msg - Output variable; error message on failure, timestamp of last logon on success
        // Returns: 
        //  bool - true if successfully logged on; false otherwise
        public bool LogOn(string user, string pass, out string msg) {
            // Check that the user actually exists.
            if (!isValidUser(user)) {
                msg = $"Username {user} not recognized. Create new account.";
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
        //  string msg - Output variable; error message on failure; confirmation on success
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
        //  string msg - Output variable; error message on failure; confirmation on success
        // Returns:
        //  bool - true if successfully withdrawn; false otherwise.
        public bool Withdraw(double amount, out string msg) {
            if (!LoggedOn) {
                msg = "Not logged in.";
                return false;
            }
            return currentAccount.Withdraw(amount, out msg);
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
