export const formatBytes = (bytes: number, decimals: number = 2) => {
  if (bytes === 0) return '0 Bytes'
  const k = 1024
  const dm = decimals < 0 ? 0 : decimals
  const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB', 'PB', 'EB', 'ZB', 'YB']

  const i = Math.floor(Math.log(bytes) / Math.log(k))
  return parseFloat((bytes / Math.pow(k, i)).toFixed(dm)) + ' ' + sizes[i]
}

export const isImageFile = (contentType: string, fileName: string) => {
  if (contentType?.startsWith('image/')) return true
  const imageExtensions = ['.jpg', '.jpeg', '.png', '.gif', '.bmp', '.webp', '.svg']
  return imageExtensions.some(ext => fileName.toLowerCase().endsWith(ext))
}

export const isOfficeFile = (fileName: string) => {
  const officeExtensions = ['.doc', '.docx', '.xls', '.xlsx', '.ppt', '.pptx']
  return officeExtensions.some(ext => fileName.toLowerCase().endsWith(ext))
}

export const isTextFile = (contentType: string, fileName: string) => {
  if (contentType === 'text/plain' || contentType === 'application/json' || contentType === 'text/csv') return true
  const textExtensions = ['.txt', '.js', '.ts', '.tsx', '.json', '.css', '.scss', '.md', '.html', '.csv', '.xml', '.log']
  return textExtensions.some(ext => fileName.toLowerCase().endsWith(ext))
}

export const isEditableFile = (fileName: string) => {
  const editableExtensions = ['.txt', '.js', '.ts', '.tsx', '.json', '.css', '.scss', '.md', '.html', '.csv', '.xml', '.log']
  return editableExtensions.some(ext => fileName.toLowerCase().endsWith(ext))
}
