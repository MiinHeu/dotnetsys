USE [master]
GO
/****** Object:  Database [NewVinhKhanhlocal]    Script Date: 5/11/2026 8:43:41 PM ******/
CREATE DATABASE [NewVinhKhanhlocal]
 CONTAINMENT = NONE
 ON  PRIMARY 
( NAME = N'NewVinhKhanhlocal_Data', FILENAME = N'C:\Users\nt\NewVinhKhanhlocal.mdf' , SIZE = 8192KB , MAXSIZE = UNLIMITED, FILEGROWTH = 1024KB )
 LOG ON 
( NAME = N'NewVinhKhanhlocal_Log', FILENAME = N'C:\Users\nt\NewVinhKhanhlocal.ldf' , SIZE = 8192KB , MAXSIZE = 2048GB , FILEGROWTH = 10%)
 WITH CATALOG_COLLATION = DATABASE_DEFAULT, LEDGER = OFF
GO
ALTER DATABASE [NewVinhKhanhlocal] SET COMPATIBILITY_LEVEL = 170
GO
IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [NewVinhKhanhlocal].[dbo].[sp_fulltext_database] @action = 'enable'
end
GO
ALTER DATABASE [NewVinhKhanhlocal] SET ANSI_NULL_DEFAULT OFF 
GO
ALTER DATABASE [NewVinhKhanhlocal] SET ANSI_NULLS OFF 
GO
ALTER DATABASE [NewVinhKhanhlocal] SET ANSI_PADDING OFF 
GO
ALTER DATABASE [NewVinhKhanhlocal] SET ANSI_WARNINGS OFF 
GO
ALTER DATABASE [NewVinhKhanhlocal] SET ARITHABORT OFF 
GO
ALTER DATABASE [NewVinhKhanhlocal] SET AUTO_CLOSE OFF 
GO
ALTER DATABASE [NewVinhKhanhlocal] SET AUTO_SHRINK OFF 
GO
ALTER DATABASE [NewVinhKhanhlocal] SET AUTO_UPDATE_STATISTICS ON 
GO
ALTER DATABASE [NewVinhKhanhlocal] SET CURSOR_CLOSE_ON_COMMIT OFF 
GO
ALTER DATABASE [NewVinhKhanhlocal] SET CURSOR_DEFAULT  GLOBAL 
GO
ALTER DATABASE [NewVinhKhanhlocal] SET CONCAT_NULL_YIELDS_NULL OFF 
GO
ALTER DATABASE [NewVinhKhanhlocal] SET NUMERIC_ROUNDABORT OFF 
GO
ALTER DATABASE [NewVinhKhanhlocal] SET QUOTED_IDENTIFIER OFF 
GO
ALTER DATABASE [NewVinhKhanhlocal] SET RECURSIVE_TRIGGERS OFF 
GO
ALTER DATABASE [NewVinhKhanhlocal] SET  ENABLE_BROKER 
GO
ALTER DATABASE [NewVinhKhanhlocal] SET AUTO_UPDATE_STATISTICS_ASYNC OFF 
GO
ALTER DATABASE [NewVinhKhanhlocal] SET DATE_CORRELATION_OPTIMIZATION OFF 
GO
ALTER DATABASE [NewVinhKhanhlocal] SET TRUSTWORTHY OFF 
GO
ALTER DATABASE [NewVinhKhanhlocal] SET ALLOW_SNAPSHOT_ISOLATION ON 
GO
ALTER DATABASE [NewVinhKhanhlocal] SET PARAMETERIZATION SIMPLE 
GO
ALTER DATABASE [NewVinhKhanhlocal] SET READ_COMMITTED_SNAPSHOT ON 
GO
ALTER DATABASE [NewVinhKhanhlocal] SET HONOR_BROKER_PRIORITY OFF 
GO
ALTER DATABASE [NewVinhKhanhlocal] SET RECOVERY FULL 
GO
ALTER DATABASE [NewVinhKhanhlocal] SET  MULTI_USER 
GO
ALTER DATABASE [NewVinhKhanhlocal] SET PAGE_VERIFY CHECKSUM  
GO
ALTER DATABASE [NewVinhKhanhlocal] SET DB_CHAINING OFF 
GO
ALTER DATABASE [NewVinhKhanhlocal] SET FILESTREAM( NON_TRANSACTED_ACCESS = OFF ) 
GO
ALTER DATABASE [NewVinhKhanhlocal] SET TARGET_RECOVERY_TIME = 60 SECONDS 
GO
ALTER DATABASE [NewVinhKhanhlocal] SET DELAYED_DURABILITY = DISABLED 
GO
ALTER DATABASE [NewVinhKhanhlocal] SET OPTIMIZED_LOCKING = OFF 
GO
ALTER DATABASE [NewVinhKhanhlocal] SET ACCELERATED_DATABASE_RECOVERY = OFF  
GO
ALTER DATABASE [NewVinhKhanhlocal] SET QUERY_STORE = ON
GO
ALTER DATABASE [NewVinhKhanhlocal] SET QUERY_STORE (OPERATION_MODE = READ_WRITE, CLEANUP_POLICY = (STALE_QUERY_THRESHOLD_DAYS = 30), DATA_FLUSH_INTERVAL_SECONDS = 900, INTERVAL_LENGTH_MINUTES = 60, MAX_STORAGE_SIZE_MB = 100, QUERY_CAPTURE_MODE = AUTO, SIZE_BASED_CLEANUP_MODE = AUTO, MAX_PLANS_PER_QUERY = 200, WAIT_STATS_CAPTURE_MODE = ON)
GO
USE [NewVinhKhanhlocal]
GO
ALTER DATABASE SCOPED CONFIGURATION SET MAXDOP = 8;
GO
USE [NewVinhKhanhlocal]
GO
/****** Object:  Table [dbo].[AppHistoryLogs]    Script Date: 5/11/2026 8:43:41 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppHistoryLogs](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[SessionId] [nvarchar](256) NOT NULL,
	[EventType] [nvarchar](50) NOT NULL,
	[PoiId] [int] NULL,
	[TourId] [int] NULL,
	[LanguageCode] [nvarchar](10) NOT NULL,
	[Payload] [nvarchar](max) NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppUsers]    Script Date: 5/11/2026 8:43:41 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppUsers](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Username] [nvarchar](256) NOT NULL,
	[PasswordHash] [nvarchar](max) NOT NULL,
	[Role] [nvarchar](50) NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[DisplayId] [nvarchar](max) NULL,
	[Email] [nvarchar](max) NULL,
	[FullName] [nvarchar](max) NULL,
	[IsActive] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[DeviceSessions]    Script Date: 5/11/2026 8:43:41 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DeviceSessions](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[SessionId] [nvarchar](128) NOT NULL,
	[DeviceModel] [nvarchar](128) NULL,
	[DevicePlatform] [nvarchar](32) NULL,
	[OsVersion] [nvarchar](32) NULL,
	[AppVersion] [nvarchar](32) NULL,
	[Manufacturer] [nvarchar](64) NULL,
	[LanguageUsed] [nvarchar](16) NULL,
	[StartedAt] [datetime2](7) NOT NULL,
	[EndedAt] [datetime2](7) NULL,
	[LastHeartbeatAt] [datetime2](7) NOT NULL,
	[PoisVisited] [int] NOT NULL,
	[DistanceMeters] [float] NOT NULL,
	[IsReturning] [bit] NOT NULL,
	[IpAddress] [nvarchar](64) NULL,
	[Country] [nvarchar](64) NULL,
	[City] [nvarchar](64) NULL,
 CONSTRAINT [PK_DeviceSessions] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[MovementLogs]    Script Date: 5/11/2026 8:43:41 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MovementLogs](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[SessionId] [nvarchar](256) NOT NULL,
	[Latitude] [float] NOT NULL,
	[Longitude] [float] NOT NULL,
	[RecordedAt] [datetime2](7) NOT NULL,
	[AccuracyMeters] [float] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Pois]    Script Date: 5/11/2026 8:43:41 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Pois](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](250) NOT NULL,
	[Description] [nvarchar](max) NOT NULL,
	[Latitude] [float] NOT NULL,
	[Longitude] [float] NOT NULL,
	[MapX] [float] NOT NULL,
	[MapY] [float] NOT NULL,
	[TriggerRadiusMeters] [float] NOT NULL,
	[Priority] [int] NOT NULL,
	[CooldownSeconds] [int] NOT NULL,
	[Category] [int] NOT NULL,
	[QrCode] [nvarchar](64) NULL,
	[IsActive] [bit] NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[UpdatedAt] [datetime2](7) NOT NULL,
	[ContentVersion] [int] NOT NULL,
	[OwnerUserId] [int] NULL,
	[OwnerInfo] [nvarchar](max) NULL,
	[ImageUrl] [nvarchar](max) NULL,
	[AudioViUrl] [nvarchar](max) NULL,
	[Address] [nvarchar](256) NULL,
	[PhoneNumber] [nvarchar](32) NULL,
	[OperatingHours] [nvarchar](64) NULL,
	[Rating] [float] NOT NULL,
	[ImagesJson] [nvarchar](max) NULL,
	[MenuJson] [nvarchar](max) NULL,
	[TagsJson] [nvarchar](max) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PoiTranslations]    Script Date: 5/11/2026 8:43:41 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PoiTranslations](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[PoiId] [int] NOT NULL,
	[LanguageCode] [nvarchar](10) NOT NULL,
	[Name] [nvarchar](250) NOT NULL,
	[Description] [nvarchar](max) NOT NULL,
	[AudioUrl] [nvarchar](max) NULL,
	[OriginalDescription] [nvarchar](max) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PoiVisitLogs]    Script Date: 5/11/2026 8:43:41 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PoiVisitLogs](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[PoiId] [int] NOT NULL,
	[SessionId] [nvarchar](256) NOT NULL,
	[LanguageCode] [nvarchar](10) NOT NULL,
	[TriggerType] [nvarchar](32) NOT NULL,
	[VisitedAt] [datetime2](7) NOT NULL,
	[ListenDurationSeconds] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Tours]    Script Date: 5/11/2026 8:43:41 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Tours](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](250) NOT NULL,
	[Description] [nvarchar](max) NULL,
	[EstimatedMinutes] [int] NOT NULL,
	[ThumbnailUrl] [nvarchar](max) NULL,
	[IsActive] [bit] NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[UpdatedAt] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[TourStops]    Script Date: 5/11/2026 8:43:41 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TourStops](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[TourId] [int] NOT NULL,
	[PoiId] [int] NOT NULL,
	[StopOrder] [int] NOT NULL,
	[StayMinutes] [int] NOT NULL,
	[Note] [nvarchar](max) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[TourTranslations]    Script Date: 5/11/2026 8:43:41 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TourTranslations](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[TourId] [int] NOT NULL,
	[LanguageCode] [nvarchar](10) NOT NULL,
	[Name] [nvarchar](250) NOT NULL,
	[Description] [nvarchar](max) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_DeviceSessions_SessionId]    Script Date: 5/11/2026 8:43:41 PM ******/
CREATE NONCLUSTERED INDEX [IX_DeviceSessions_SessionId] ON [dbo].[DeviceSessions]
(
	[SessionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_DeviceSessions_StartedAt]    Script Date: 5/11/2026 8:43:41 PM ******/
CREATE NONCLUSTERED INDEX [IX_DeviceSessions_StartedAt] ON [dbo].[DeviceSessions]
(
	[StartedAt] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[AppUsers] ADD  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[DeviceSessions] ADD  DEFAULT ((0)) FOR [PoisVisited]
GO
ALTER TABLE [dbo].[DeviceSessions] ADD  DEFAULT ((0)) FOR [DistanceMeters]
GO
ALTER TABLE [dbo].[DeviceSessions] ADD  DEFAULT ((0)) FOR [IsReturning]
GO
ALTER TABLE [dbo].[Pois] ADD  DEFAULT ((0)) FOR [ContentVersion]
GO
ALTER TABLE [dbo].[Pois] ADD  DEFAULT ((5.0)) FOR [Rating]
GO
ALTER TABLE [dbo].[PoiTranslations]  WITH CHECK ADD FOREIGN KEY([PoiId])
REFERENCES [dbo].[Pois] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[PoiVisitLogs]  WITH CHECK ADD FOREIGN KEY([PoiId])
REFERENCES [dbo].[Pois] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[TourStops]  WITH CHECK ADD FOREIGN KEY([PoiId])
REFERENCES [dbo].[Pois] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[TourStops]  WITH CHECK ADD FOREIGN KEY([TourId])
REFERENCES [dbo].[Tours] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[TourTranslations]  WITH CHECK ADD FOREIGN KEY([TourId])
REFERENCES [dbo].[Tours] ([Id])
ON DELETE CASCADE
GO
USE [master]
GO
ALTER DATABASE [NewVinhKhanhlocal] SET  READ_WRITE 
GO
