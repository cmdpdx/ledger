# Ledger
Simple transaction ledger for multiple accounts. For basic console usage, include `using Ledger.Interact;` and `Run` an instance of `CLI`:
```
using Ledger.Interact;
class Example {
    public static void Main() {
        var cli = new CLI();
        cli.Run()
    }
}
```

## Namespace: Ledger
* **Account**: Transaction ledger for a single account
* **AccountManager**: Managers logging onto/off of accounts and interacting with accounts.

### class Account 
#### Constructor
**Account(string userName, string password)**: Create an Account object with the supplied userName and password. Password is stored hashed using SHA256.

#### Public Properties
* **_string_ UserName**: Name associated with the account
* **_double_ Balance**: Current account balance
* **_DateTime_ LastLogOn**: DateTime of last logon to the account. Set on creation and by calling UpdateLastLogOn().
* **_List\<string\>_ TransactionHistory**: List of strings of all transactions recorded on the account.

#### Public Methods
* **_bool_ Deposit(double amount, out string msg)**: Record a deposit on the account. Returns true/false to indicate success of deposit. Output parameter msg contains future information.
* **_bool_ Withdraw(double amount, out string msg)**: Record a withdrawl on the account. Returns true/false to indicate success of withdrawl. Output parameter msg contains future information.
* **_bool_ CheckPassword(string password)**: Tests if the supplied password's hash matches the account's password's hash.
* **_void_ UpdateLastLogOn()**: Sets LastLogOn to DateTime.Now.
