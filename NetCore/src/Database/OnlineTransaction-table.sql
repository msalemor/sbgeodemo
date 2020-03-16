drop table OnlineTransaction
go

create table OnlineTransaction
(
	Id int not null primary key identity,
	[Type] varchar(20) not null default 'SO',
	[No] varchar(20) not null,
	CreatedTimeUTC DateTimeOffset not null default GetUTCDate(),
	CustomerId varchar(20) not null,
	Total money not null default 0,
	ProcessedTimeUTC DateTimeOffset,
	Processed bit not null default 0,
	[Json] nvarchar(max),
	[Version] varchar(10) not null default '1.0'
)
go

insert into dbo.OnlineTransaction values ('SO','Number',default,'customer',0.0,null,0,null,default)
go

select * from OnlineTransaction
go

truncate table OnlineTransaction
go