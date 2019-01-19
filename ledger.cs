using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

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


    // TODO: add reference to currentAccount, rather than coding the pathways through.
    class AccountManager {
        private Account currentAccount;
        private List<Account> accounts;

        // constructor
        public AccountManager() {
            accounts = new List<Account>();
            currentAccount = null;
        }

        // properties
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

        // public methods
        public bool CreateAccount(string user, string pass, out string msg) {
            // does this username already exist?
            if (isValidUser(user)) {
                msg = $"Username {user} already exists.";
                return false;
            }
            msg = $"Account created: {user}";
            currentAccount = new Account(user, pass);
            accounts.Add(currentAccount);
            return true;
        }

        public bool LogOn(string user, string pass, out string msg) {
            // check that the user actually exists
            if (!isValidUser(user)) {
                msg = $"Username {user} not recognized. Create new account.";
                return false;
            }
            // find the correct account, check the password supplied
            var account = accounts.Find(x => x.UserName.Equals(user));
            if (!account.CheckPassword(pass)) {
                msg = "Invalid password.";
                return false;
            }
            
            currentAccount = account;
            msg = currentAccount.LastLogOn.ToString();
            currentAccount.UpdateLastLogOn();
            return true;
        }

        public void LogOut() {
            //currentAccount.store();
            currentAccount = null;
        }

        public bool Deposit(double amount, out string msg) {
            if (!LoggedOn) {
                msg = "Not logged in.";
                return false;
            }
            return currentAccount.Deposit(amount, out msg);
        }

        public bool Withdraw(double amount, out string msg) {
            if (!LoggedOn) {
                msg = "Not logged in.";
                return false;
            }
            return currentAccount.Withdraw(amount, out msg);
        }

        // private methods
        private bool isValidUser(string user) {
            foreach (var account in accounts) {
                if (account.UserName.Equals(user))
                    return true;
            }
            return false;
        }
    }

    // TODO: If user is not logged in, only give options to log on or add new user.
    class CLI {
        private AccountManager manager;
        private Dictionary<string, Action> options;

        public CLI() {
            manager = new AccountManager();
            options = new Dictionary<string, Action>();
            options["new"] = newUser;
            options["logon"] = logOnUser;
            options["logout"] = logOutUser;
            options["balance"] = balance;
            options["deposit"] = deposit;
            options["withdraw"] = withdraw;
            options["history"] = history;
            options["help"] = help;

        }

        public void Run() {
            string input = string.Empty;
            Console.WriteLine("Transaction Ledger.");
            Console.WriteLine("Create new account with \"new\". Type \"help\" to see all options.");
            while (true) {
                Console.Write($"{manager.LoggedOn ? manager.UserName : ""}>");
                input = Console.ReadLine();

                // Ensure user cannot perform actions that require login without being logged on.
                // ...feels like there is a better way, but I'll go with this for now...
                if (!manager.LoggedOn && (input.Equals("balance") || input.Equals("deposit") || input.Equals("withdraw") || input.Equals("history"))) {
                    Console.WriteLine($"Must be logged on to perform \"{input}\".");
                } else if (input.Equals("quit")) {
                    break;
                } else if (!options.ContainsKey(input)) {
                    Console.WriteLine($"Option \"{input}\" not recognized."); 
                } else {
                    options[input]();
                }
                
                Console.WriteLine();
            } 
        }

        // TODO: load existing users from persistant storage
        private void loadUsers() {

        }

        private void help() {
            Console.WriteLine("Always available: new, logon, logout, help, quit");
            Console.WriteLine("When logged on: balance, deposit, withdraw, history");
        }
        private void newUser() {
            if (manager.LoggedOn) {
                Console.Write($"Already logged in as user {manager.UserName}, logout (y/n)? ");
                string input = Console.ReadLine();
                if (!input.Equals("y"))
                    return;
                logOutUser();
            }

            Console.Write("Choose username: ");
            string user = Console.ReadLine();
            Console.Write("Choose password: ");
            string pass = Console.ReadLine();
            
            bool success = manager.CreateAccount(user, pass, out string msg);
            if (!success) 
                Console.WriteLine($"User creation failed: {msg}");
        }

        private void logOnUser() {
            if (manager.LoggedOn) {
                Console.Write($"Already logged on as user {manager.UserName}, logout (y/n)? ");
                string input = Console.ReadLine();
                if (!input.Equals("y"))
                    return;
                logOutUser();
            }

            Console.Write("username: ");
            string user = Console.ReadLine();
            Console.Write("password: ");
            string pass = Console.ReadLine();

            bool success = manager.LogOn(user, pass, out string msg);
            if (!success)
                Console.WriteLine($"Log on failed: {msg}");
            else
                Console.WriteLine($"User {user} logged on. Last Log on at {msg}");
        }

        private void logOutUser() {
            if (!manager.LoggedOn) 
                return;
            
            manager.LogOut();
        }

        private void balance() {
            if (!manager.LoggedOn) {
                Console.WriteLine("Must be logged on to view balance.");
                return;
            }

            Console.WriteLine($"Current balance: ${manager.Balance}");
        }

        private void deposit() {
            if (!manager.LoggedOn) {
                Console.WriteLine("Must be logged in to deposit.");
                return;
            }

            Console.Write("Despoit amount > ");
            string input = Console.ReadLine();
            double amount;
            if (!Double.TryParse(input, out amount)) {
                Console.WriteLine($"Invalid deposit input: {input}.");
                return;
            }

            bool success = manager.Deposit(amount, out string msg);
            if (!success)
                Console.WriteLine($"Deposit failed: {msg}");            
        }

        private void withdraw() {
            if (!manager.LoggedOn) {
                Console.WriteLine("Must be logged in to withdraw.");
                return;
            }

            Console.Write("Withdrawl amount > ");
            string input = Console.ReadLine();
            double amount;
            if (!Double.TryParse(input, out amount)) {
                Console.WriteLine($"Invalid withdrawl input: {input}.");
                return;
            }

            bool success = manager.Withdraw(amount, out string msg);
            if (!success) 
                Console.WriteLine($"Withdrawl failed: {msg}");    
        }

        private void history() {
            if (!manager.LoggedOn) {
                Console.WriteLine("Must be logged in to withdraw.");
                return;
            }

            Console.WriteLine($"Transaction history for user {manager.UserName}:");
            var transactions = manager.TransactionHistory;
            foreach (var t in transactions) {
                Console.WriteLine(t);
            }
        }
    }
}
