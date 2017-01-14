/****** Object:  Table [dbo].[Audit]    Script Date: 02/06/2008 09:30:00 ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Audit]') AND type in (N'U'))
DROP TABLE [dbo].Audit
GO
/****** Object:  Table [dbo].[Audit]    Script Date: 02/06/2008 09:30:00 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Audit]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[Audit](
	[AuditID] [int] IDENTITY(1,1) NOT NULL,
	[UserTrack] [varchar](255) NULL,
	[Machine] [varchar](50) NULL,
	[ApplicationName] [varchar](50) NULL,
	[MethodName] [varchar](50) NULL,
	[Parameters] [varchar](1024) NULL,
	[ParametersRaw] [image] NULL,
	[Source] [varchar](250) NULL,
	[Billing] [int] NULL CONSTRAINT [DF_Audit_Billing] DEFAULT (0),
	[CreateDate] [datetime] NULL CONSTRAINT [DF_Audit_CreateDate]  DEFAULT (getdate()),
	[Signature] [image] NULL
 CONSTRAINT [PK_Audit] PRIMARY KEY CLUSTERED 
(
	[AuditID] ASC
)WITH (IGNORE_DUP_KEY = OFF))
END
GO
/****** Object:  Table [dbo].[Audit]    Script Date: 02/06/2008 09:30:00 ******/
