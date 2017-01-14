/****** Object:  StoredProcedure [dbo].[DelAuditByDate]    Script Date: 07/10/2007 09:50:15 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DelAuditByDate]') AND type in (N'P', N'PC'))
BEGIN
EXEC dbo.sp_executesql @statement = N'

CREATE PROCEDURE [dbo].[DelAuditByDate]
    @Cutoff datetime
AS
BEGIN

    SET NOCOUNT ON;
    
    -- This Proc will delete all audit records older than the specified date
    Delete from audit where Billing = 0 and createdate < @Cutoff
    
END

' 
END
GO
/****** Object:  StoredProcedure [dbo].[DelAuditByDate]    Script Date: 07/10/2007 09:50:15 ******/
