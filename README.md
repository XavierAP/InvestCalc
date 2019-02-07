# InvestCalc
Program to keep track of investments and calculate their returns.

![Screenshot](docs/screenshots/example_main.png "Example of main window")

## Features
* Registers orders
(buy/sell shares, dividends/costs).

* Persistent disk database
(portfolio and order data persist between sessions).

* Displays the current portfolio
(currently owned shares of every stock).

* Displays order history.
Can filter by dates and stocks.

* Calculates the equivalent yearly return of each investment,
and all investments together.
This is done after the user has entered the current price(s).
The equivalent yearly return is defined as the interest/discount rate
that makes the net present value of all cash flows
equal to the current value
(current price multiplied by number of shares currently owned).

## Upcoming features
* Manipulate order history: delete records.
* Import/export order history from/to CSV.
* Port as mobile app...

## Notes

* [**`Data_definition.sql`**](Data_definition.sql)
contains the SQLite database definition.
This file must be deployed in the application directory:
it would be read to create a fresh database
in case an existing one is not found for the user.

## Dependencies
* [libcs-math](https://github.com/XavierAP/libcs-math)
is used to calculate the returns,
by the iterative Newton's method.

* [libcs-sqlite](https://github.com/XavierAP/libcs-sqlite)
is used by
the [**`Data`**](Data.cs) class
for all SQL database features.
