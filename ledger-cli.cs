using System;
using System.Collections.Generic;

namespace Ledger {
    public class CLI {
        private AccountManager manager;
        private Dictionary<string, Action> options;

        public CLI() {
            manager = new AccountManager();
            options = new Dictionary<string, Action>();
            options["new"] = newUser;
            options["logon"] = logOnUser;
            options["logout"] = logOutUser;
            options["save accounts"] = saveAccounts;
            options["load accounts"] = loadAccounts;
            options["balance"] = balance;
            options["deposit"] = deposit;
            options["withdraw"] = withdraw;
            options["history"] = history;
            options["help"] = help;
            options["quit"] = () => {};

        }

        public void Run() {
            string input = string.Empty;
            Console.WriteLine("Transaction Ledger.");
            Console.WriteLine("Create new account with \"new\". Type \"help\" to see all options.");
            while (!input.Equals("quit")) {
                Console.Write($"{manager.LoggedOn ? manager.UserName : ""}>");
                input = Console.ReadLine();

                // Ensure user cannot perform actions that require login without being logged on.
                // ...feels like there is a better way, but I'll go with this for now...
                if (!manager.LoggedOn && requiresLogOn(input)) {
                    Console.WriteLine($"Must be logged on to perform \"{input}\".");
                } else if (!options.ContainsKey(input)) {
                    Console.WriteLine($"Option \"{input}\" not recognized."); 
                } else {
                    options[input]();
                }
                
                Console.WriteLine();
            } 
        }

        private bool requiresLogOn(string option) {
            if (option.Equals("deposit") ||
                option.Equals("withdraw") ||
                option.Equals("balance") ||
                option.Equals("history") ) 
                return true;

            return false;
        }

        // TODO: load existing users from persistant storage
        private void loadUsers() {

        }

        private void help() {
            Console.WriteLine("Always available: new, logon, logout, load accounts, save accounts, help, quit");
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

        private void loadAccounts() {
            Console.Write("Filename >");
            string fileName = Console.ReadLine();
            string msg;
            manager.LoadAccounts(fileName, out msg);
            Console.WriteLine(msg);
        }

        private void saveAccounts() {
            Console.Write("Filename >");
            string fileName = Console.ReadLine();
            string msg;
            manager.SaveAccounts(fileName, out msg);
            Console.WriteLine(msg);
        }

        private void balance() {
            Console.WriteLine($"Current balance: ${manager.Balance}");
        }

        private void deposit() {
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
            Console.WriteLine($"Transaction history for user {manager.UserName}:");
            var transactions = manager.TransactionHistory;
            foreach (var t in transactions) {
                Console.WriteLine(t);
            }
        }
    }
}
