/****** Object:  StoredProcedure [dbo].[GetCountByDate]    Script Date: 07/10/2007 09:50:15 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[GetCountByDate]') AND type in (N'P', N'PC'))
BEGIN
EXEC dbo.sp_executesql @statement = N'

CREATE PROCEDURE [dbo].[GetCountByDate]
    @Counter int, 
    @DateFirst datetimeoffset,
    @DateLast datetimeoffset
AS
BEGIN
    SET NOCOUNT ON;
	Select Count(Counter) as Total
	From Count
	Where Counter = @Counter and CreateDate >= @DateFirst and CreateDate < @DateLast
END

' 
END
GO
/****** Object:  StoredProcedure [dbo].[GetCountRange]    Script Date: 07/10/2007 09:50:15 ******/
