/* Database definition.
 * This script file must be in the same folder together
 * with the application executable, because it is used
 * by the later to create a fresh database when needed.
 */
pragma foreign_keys = ON;

create table Stocks ( -- Portfolio of stocks, shares, bonds, metals, etc.
	id integer primary key,
	name text unique collate nocase not null -- Human-friendly name; unique index.
);
create table Flows ( -- Record of purchases, sales, dividends, holding costst, etc.
	utcDate integer not null,
	stock integer not null references Stocks(id)
		on delete restrict on update cascade,
	shares real not null, -- Amount of shares acquired (+) or sold or forfeit (-). May be non integer in case of e.g. physical metals.
	flow real not null, -- Money received (+) or paid (-); in principle opposite sign to 'shares'.
	comment text
);
create index idxFlow2Stock
on Flows(stock);