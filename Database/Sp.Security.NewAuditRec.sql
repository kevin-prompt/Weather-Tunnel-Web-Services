/****** Object:  StoredProcedure [dbo].[NewAuditRec]    Script Date: 02/06/2008 09:30:00 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[NewAuditRec]') AND type in (N'P', N'PC'))
BEGIN
EXEC dbo.sp_executesql @statement = N'     
CREATE PROCEDURE [dbo].[NewAuditRec] ( 	
  @UserTrack varchar(255), 
  @Machine varchar(50), 
  @ApplicationName varchar(50), 
  @MethodName varchar(50), 
  @Parameters varchar(1024), 
  @ParametersRaw image = NULL,
  @Source varchar(50),
  @Billing int,
  @Signature image = NULL
  ) 
  
  AS SET NOCOUNT ON  
  
  insert into Audit (UserTrack, Machine, ApplicationName, MethodName, Parameters, ParametersRaw, Source, Billing, Signature) 
         values (@UserTrack, @Machine, @ApplicationName, @MethodName, @Parameters, @ParametersRaw, @Source, @Billing, @Signature)
  
' 
END
GO
/****** Object:  StoredProcedure [dbo].[NewAuditRec]    Script Date: 02/06/2008 09:30:00 ******/
