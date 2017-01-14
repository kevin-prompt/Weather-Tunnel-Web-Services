/****** Object:  StoredProcedure [dbo].[DelAuditInfo]    Script Date: 07/10/2007 09:50:15 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DelAuditInfo]') AND type in (N'P', N'PC'))
BEGIN
EXEC dbo.sp_executesql @statement = N'
CREATE PROCEDURE [dbo].[DelAuditInfo]
    @AuditID int 	  
AS 
BEGIN
    SET NOCOUNT ON;
    delete from Audit where AuditID = @AuditID 
END 
' 
END
GO
/****** Object:  StoredProcedure [dbo].[DelAuditInfo]    Script Date: 07/10/2007 09:50:15 ******/
