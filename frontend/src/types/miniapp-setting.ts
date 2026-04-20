export type MiniAppThemeCode = 'green' | 'light' | 'candy' | 'orange' | 'red'

export interface MiniAppThemeSettingsDto {
  themeCode: MiniAppThemeCode
}

export interface SaveMiniAppThemeSettingsRequest {
  themeCode: MiniAppThemeCode
}
