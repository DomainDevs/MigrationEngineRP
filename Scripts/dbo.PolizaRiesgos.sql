/* 
=================================================================
-- MIGRACION SISTRAN
--Autor: Sistran
--Fecha Creación:       20260301
--Fecha Modificación:   20260303
--Funcionalidad:        Crear objeto [dbo].[PolizaRiesgos]
=================================================================
*/
IF OBJECT_ID('[dbo].[PolizaRiesgos]') IS NOT NULL
BEGIN
    DROP TABLE [dbo].[PolizaRiesgos]
    IF OBJECT_ID('[dbo].[PolizaRiesgos]') IS NOT NULL
        PRINT '<<< FAILED DROPPING TABLE [dbo].[PolizaRiesgos] >>>'
    ELSE
        PRINT '<<< DROPPED TABLE [dbo].[PolizaRiesgos] >>>'
END
GO
CREATE TABLE [dbo].[PolizaRiesgos](
	[Id_pv] [int] NOT NULL,
	[Cod_Riesgo] [int] NOT NULL,
	[TipoRiesgo] [nvarchar](100) NOT NULL,
	[Descripcion] [nvarchar](500) NULL,
	[SumaAsegurada] [decimal](18, 2) NULL,
	[PrimaRiesgo] [decimal](18, 2) NULL,
    CONSTRAINT [PK_PolizaRiesgos] PRIMARY KEY CLUSTERED 
    (
     [Id_pv] ASC, [Cod_Riesgo] ASC
    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

IF EXISTS (
    SELECT 1
    FROM sysobjects
    WHERE id = OBJECT_ID('dbo.PolizaRiesgos')
    AND type = 'U'
)
BEGIN
    PRINT '<<CREATE TABLE dbo.PolizaRiesgos SUCCESSFUL!! >>'
END
GO