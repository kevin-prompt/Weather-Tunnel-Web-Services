/****** Object:  StoredProcedure [dbo].[ChgAuditBill]    Script Date: 02/13/2009 09:51:08 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ChgAuditBill]') AND type in (N'P', N'PC'))
BEGIN
EXEC dbo.sp_executesql @statement = N'   CREATE PROCEDURE [dbo].[ChgAuditBill] 
(
	@ChgAuditBillID int,
	@updateBilling int
)
AS 
SET NOCOUNT ON   
Update Audit Set Billing = @updateBilling
Where AuditID = @ChgAuditBillID
RETURN    ' 
END
GO
/****** Object:  StoredProcedure [dbo].[ChgAuditBill]    Script Date: 02/13/2009 09:51:08 ******/
