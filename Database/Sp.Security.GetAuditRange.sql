/****** Object:  StoredProcedure [dbo].[GetAuditRange]    Script Date: 07/10/2007 09:50:15 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[GetAuditRange]') AND type in (N'P', N'PC'))
BEGIN
EXEC dbo.sp_executesql @statement = N'

CREATE PROCEDURE [dbo].[GetAuditRange]
    @WhereClause varchar(1000), 
    @SortBy varchar(1000),
    @SortOrder varchar(1000)
AS
BEGIN
    SET NOCOUNT ON;
    -- This Proc uses a dynamically generated where and order by clause.
    DECLARE @sExec varchar(8000)

    SET @sExec = ''SELECT auditid from audit where '' + @WhereClause + '' ORDER BY '' + @SortBy + '' '' + @SortOrder

    EXEC(@sExec)
END

' 
END
GO
/****** Object:  StoredProcedure [dbo].[GetAuditRange]    Script Date: 07/10/2007 09:50:15 ******/
