
DECLARE @RC int
DECLARE @Counter int
DECLARE @DateFirst datetimeoffset(7)
DECLARE @DateLast datetimeoffset(7)

set @Counter = 1
set @DateFirst = '2013-12-15T05:00:00.0000000+00:00'
set @DateLast = getutcdate()

EXECUTE @RC = [dbo].[GetCountByDate] 
   @Counter
  ,@DateFirst
  ,@DateLast

GO


