#bank card stuff
bank-card-name = {$name}'s bank card
bank-card-description = This bank card belongs to {$name}.
bank-card-slot-name = Bank Card
bank-card-examine-account-number = The account number is {$number}
bank-card-examine-balance = The account's balance is {$balance} scrip
bank-card-examine-salary = The account's salary is {$salary} scrip
bank-pin-notify = Your bank account is #{$account}. Your PIN is {$pin} - keep it secret!

#atm stuff
atm-machine-window-title = Automated Teller Machine
atm-machine-account-name-title = Welcome, {$name}!
atm-machine-account-access-number-title = Access Number: {$number}
atm-machine-account-transfer-number-title = Transfer Number: {$number}
atm-machine-account-balance-title = Balance: ${$balance}
atm-machine-no-transactions = This Account Has Made No Transactions
atm-invalid-account-number = Please insert a bank card to continue
atm-card-unprogrammed = This card is not programmed, take it to the Head of Personnel!
atm-insert-card-button = Insert Card
atm-eject-card-button = Eject Card
atm-deposit-button = Deposit
atm-withdraw-button = Withdraw

#NanoBank branded stuff
nanobank-title = NanoBank
nanobank-tagline = The only bank you'll need.
nanobank-welcome = Welcome, {$name}!
nanobank-balance-label = Balance
nanobank-balance-amount = N$ {$balance}
nanobank-recent-transactions = Recent Transactions
nanobank-transaction-amount-in = +N$ {$amount}
nanobank-transaction-amount-out = -N$ {$amount}
nanobank-transaction-counterparty = {$name} #{$number}
nanobank-category-purchase = Purchase
nanobank-category-deposit = Deposit
nanobank-transaction-tooltip-in = You received N$ {$amount} from {$name} (#{$number}) for "{$reason}"
nanobank-transaction-tooltip-out = You sent N$ {$amount} to {$name} (#{$number}) for "{$reason}"

transactions-container-title = Transactions
transfer-funds-button-title = Transfer Funds

#transaction window
transaction-window-title = Transfer Funds
atm-recipient-transfer-number = Recipient:
atm-transfer-amount = Amount:
atm-transfer-reason = Reason:
atm-transfer-reason-charcount = {$count} Characters Remaining
transaction-low-funds = Error : Not Enough Funds
transaction-no-recipient = Error : Recipient Does Not Exist
atm-cancel-button-label = Cancel
atm-confirm-button-label = Confirm
atm-really-confirm-label = Really Confirm

#pos system
pos-window-title = Point-of-sale system
pos-begin-setup-text = This device has not been set up, please press the button below to begin
pos-begin-setup-present-card = This device has not been set up, please enter a valid account number or present a valid bank card to continue
pos-begin-setup-button-text = Begin Setup
pos-setup-recipient-account-number = Recipient Account Number
pos-setup-charge-amount = Charge Amount
pos-setup-reason = Reason
pos-setup-err-invalid-recipient = Error : invalid recipient
pos-setup-err-invalid-transfer-amount = Error : please specify charge amount
pos-setup-confirmed = settings updated!
pos-clear-setup-button-label = Clear Setup
pos-confirm-setup-button-label = Confirm

pos-payment-present-card = Please enter a valid account number or present a valid bank card to continue
pos-payment-name-and-number = {$name} (#{$number})
pos-payment-is-trying-to-charge = Is trying to charge you
pos-payment-spesito-amount = ${$amount} Spesitos
pos-payment-reason = for "{$reason}"
pos-payment-confirm-button-label = Confirm Transaction
pos-payment-cancel-button-label = Cancel

#account management console
account-management-window-title = Payment Records Computer
nanobank-station-balance = Station Balance
nanobank-next-cycle = Next Cycle
nanobank-account-records = Account Records
nanobank-search-placeholder = Search name or number...
nanobank-tab-accounts = Accounts
nanobank-tab-departments = Departments
nanobank-select-account = Select an account.
nanobank-suspend-dept-button = Suspend
nanobank-resume-dept-button = Resume
nanobank-status-label = Status
nanobank-status-eligible = Eligible
nanobank-status-suspended = Suspended
nanobank-set-suspended-button = Suspend Payments
nanobank-set-eligible-button = Resume Payments
nanobank-reason-placeholder = Reason
nanobank-current-pay-label = Current Pay
nanobank-pay-per-cycle = {$amount} Scr/c
nanobank-set-pay-button = Set New Pay
nanobank-grant-bonus-label = Grant Bonus
nanobank-grant-bonus-button = Grant Bonus
nanobank-input-placeholder = input
nanobank-account-number = #{$number}
nanobank-unknown-account = Unknown
nanobank-placeholder = ---
nanobank-station-bank = Station Bank
nanobank-bonus-reason = Bonus
nanobank-cash = Cash
nanobank-deposit-reason = Deposit
nanobank-withdrawal-reason = Withdrawal
nanobank-salary-reason = Salary
nanobank-withheld = Withheld
nanobank-withheld-reason = Withheld: {$reason}
nanobank-withheld-wanted = Wanted
nanobank-withheld-detained = Detained
nanobank-payout-announcement = Pay is out! Check your salaries for correct amounts.
nanobank-payout-sender = Station

#currency exchange
currency-exchange-window-title = Currency Exchange
currency-exchange-slot-name = Cash
currency-exchange-title = Spesos {"<->"} Scrip
currency-exchange-rate = Conversion tax: {$tax}%
currency-exchange-empty = Insert spesos or scrip to begin.
currency-exchange-inserted = Inserted: {$count} {$currency}
currency-exchange-preview = → {$amount} {$currency}
currency-exchange-spesos = spesos
currency-exchange-scrip = scrip
currency-exchange-insert-button = Insert Cash
currency-exchange-eject-button = Eject
currency-exchange-convert-button = Convert

#card programming
nanobank-program-card-button = Program Card
nanobank-program-card-title = Program Bank Card
nanobank-back-button = Back
nanobank-write-card-button = Write Account To Card
nanobank-card-target = Writing: {$name}
nanobank-no-account-selected = no account selected
nanobank-card-slot-empty = No card inserted
nanobank-card-slot-filled = Card inserted
