/****** Object:  StoredProcedure [dbo].[NewCountRec]    Script Date: 02/06/2008 09:30:00 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[NewCountRec]') AND type in (N'P', N'PC'))
BEGIN
EXEC dbo.sp_executesql @statement = N'     
CREATE PROCEDURE [dbo].[NewCountRec] ( 	
  @Counter int
  ) 
  
  AS SET NOCOUNT ON  
  
  insert into Count (Counter) 
         values (@Counter)
  
' 
END
GO
/****** Object:  StoredProcedure [dbo].[NewCountRec]    Script Date: 02/06/2008 09:30:00 ******/
