type MissingTranslationListener = (keys: string[]) => void

const missingKeys = new Set<string>()
const listeners = new Set<MissingTranslationListener>()

const emit = () => {
    const values = [...missingKeys].sort((left, right) => left.localeCompare(right))
    for (const listener of listeners) {
        listener(values)
    }
}

export const trackMissingTranslationKey = (key: string) => {
    const normalized = key?.trim()
    if (!normalized) {
        return
    }

    if (!missingKeys.has(normalized)) {
        missingKeys.add(normalized)
        emit()
    }
}

export const clearMissingTranslationKeys = () => {
    if (missingKeys.size === 0) {
        return
    }

    missingKeys.clear()
    emit()
}

export const subscribeMissingTranslationKeys = (listener: MissingTranslationListener) => {
    listeners.add(listener)
    listener([...missingKeys].sort((left, right) => left.localeCompare(right)))

    return () => {
        listeners.delete(listener)
    }
}

export const getMissingTranslationKeys = () => {
    return [...missingKeys].sort((left, right) => left.localeCompare(right))
}
