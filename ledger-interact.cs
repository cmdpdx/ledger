using Ledger;
using System;
using System.Collections.Generic;

namespace Ledger.Interact {

    // Command line interface for interacting with a Ledger.AccountManager.
    // Gives access to all public functionality of an AccountManager.
    public class CLI {
        private AccountManager manager;
        // Maps the user-typed command with the method to perform
        private Dictionary<string, Action> commands;

        public CLI() {
            manager = new AccountManager();
            commands = new Dictionary<string, Action>();
            // Initilize the dictionary with available commands
            commands["new"] = newUser;
            commands["logon"] = logOnUser;
            commands["logout"] = logOutUser;
            commands["save accounts"] = saveAccounts;
            commands["load accounts"] = loadAccounts;
            commands["balance"] = balance;
            commands["deposit"] = deposit;
            commands["withdraw"] = withdraw;
            commands["history"] = history;
            commands["help"] = help;
            commands["quit"] = () => {};
        }

        // Run the command line interface for an AccountManager. This is the only way to 
        // interact with the CLI instance.
        public void Run() {
            string input = string.Empty;
            Console.WriteLine("Transaction Ledger.");
            Console.WriteLine("Create new account with \"new\". Type \"help\" to see all commands.");
            while (!input.Equals("quit")) {
                Console.Write($"{manager.LoggedOn ? manager.UserName : ""}>");
                input = Console.ReadLine();
                // for future implementation of arguments to commands
                string [] split = input.Split(' ');
                string command = split[0];
                string data = split.Length > 1 ? split[1] : string.Empty;

                // Ensure user cannot perform actions that require login without being logged on.
                // ...feels like there is a better way, but I'll go with this for now...
                if (!manager.LoggedOn && requiresLogOn(command)) {
                    Console.WriteLine($"Must be logged on to perform \"{command}\".");
                } else if ((command.Equals("deposit") || command.Equals("withdraw")) && !data.Equals(string.Empty)) {
                    if (command.Equals("deposit")) 
                        deposit(data);
                    else
                        withdraw(data);
                } else if (!commands.ContainsKey(command)) {
                    Console.WriteLine($"Option \"{command}\" not recognized."); 
                } else {
                    commands[command]();
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

        private void help() {
            Console.WriteLine("Always available: new, logon, logout, load accounts, save accounts, help, quit");
            Console.WriteLine("When logged on: balance, deposit, withdraw, history");
            Console.WriteLine("\"deposit\" and \"withdraw\" may include amount inline (e.g. \"deposit 100\").");
        }

        private void newUser() {
            // If manager is already logged on, force log out before creating new user (or cancel user creation)
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
            // If manager is already logged on, force log out before logging onto a new account (or cancel log on)
            if (manager.LoggedOn) {
                Console.Write($"Already logged on as user {manager.UserName}, logout (y/n)? ");
                string input = Console.ReadLine();
                if (!input.Equals("y"))
                    return;
                logOutUser();
            }

            Console.Write("Username: ");
            string user = Console.ReadLine();
            Console.Write("Password: ");
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
            deposit(Console.ReadLine());
        }
            
        private void deposit(string input) {
            double amount;
            if (!Double.TryParse(input, out amount)) {
                Console.WriteLine($"Invalid deposit input: {input}.");
                return;
            }

            bool success = manager.Deposit(amount, out string msg);
            if (!success)
                Console.WriteLine($"Deposit failed: {msg}");  
            else
                Console.WriteLine(msg);          
        }

        private void withdraw() {
            Console.Write("Withdrawl amount > ");
            withdraw(Console.ReadLine());
        }
        
        private void withdraw(string input) {
            double amount;
            if (!Double.TryParse(input, out amount)) {
                Console.WriteLine($"Invalid withdrawl input: {input}.");
                return;
            }

            bool success = manager.Withdraw(amount, out string msg);
            if (!success) 
                Console.WriteLine($"Withdrawl failed: {msg}");  
            else
                Console.WriteLine(msg);  
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
