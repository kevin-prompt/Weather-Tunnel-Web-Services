/****** Object:  Table [dbo].[Count]    Script Date: 02/06/2008 09:30:00 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Count]') AND type in (N'U'))
DROP TABLE [dbo].Count
GO
/****** Object:  Table [dbo].[Count]    Script Date: 02/06/2008 09:30:00 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Count]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[Count](
	[CountID] [bigint] IDENTITY(1,1) NOT NULL,
	[Counter] [int] NOT NULL,
	[CreateDate] [datetimeoffset] NOT NULL CONSTRAINT [DF_Count_CreateDate]  DEFAULT (getutcdate())
 CONSTRAINT [PK_Count] PRIMARY KEY CLUSTERED 
(
	[CountID] ASC,
	[Counter] ASC,
	[CreateDate] ASC
)WITH (IGNORE_DUP_KEY = OFF))
END
GO
/****** Object:  Table [dbo].[Audit]    Script Date: 02/06/2008 09:30:00 ******/
