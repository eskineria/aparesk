import { FormControl, FormLabel } from 'react-bootstrap'
import { LuKeyRound } from 'react-icons/lu'
import { useTranslation } from 'react-i18next'

type PasswordInputProps = {
  password: string
  setPassword: (value: string) => void
  showIcon?: boolean
  id?: string
  name?: string
  placeholder?: string
  label?: string
  labelClassName?: string
  inputClassName?: string
  error?: string
}

const calculatePasswordStrength = (password: string): number => {
  let strength = 0
  if (password.length >= 8) strength++
  if (/[A-Z]/.test(password)) strength++
  if (/\d/.test(password)) strength++
  if (/[\W_]/.test(password)) strength++
  return strength
}

const PasswordInputWithStrength = ({
  password,
  setPassword,
  id,
  label,
  name,
  placeholder,
  showIcon,
  labelClassName,
  inputClassName,
  error,
}: PasswordInputProps) => {
  const { t } = useTranslation();
  const strength = calculatePasswordStrength(password)
  const strengthBars = new Array(4).fill(0)

  return (
    <>
      {label && (
        <FormLabel htmlFor={id} className={labelClassName}>
          {label} <span className="text-danger">*</span>
        </FormLabel>
      )}

      <div className="input-group">
        <FormControl
          type="password"
          name={name}
          id={id}
          placeholder={placeholder}
          className={inputClassName}
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          isInvalid={!!error}
        />
        {showIcon && (
          <LuKeyRound className="app-search-icon text-muted" />
        )}
        <FormControl.Feedback type="invalid">
          {error}
        </FormControl.Feedback>
      </div>

      <div className="password-bar my-2">
        {strengthBars.map((_, i) => (
          <div key={i} className={'strong-bar ' + (i < strength ? `bar-active-${strength}` : '')} />
        ))}
      </div>

      <p className="text-muted fs-xs mb-0">{t('auth.register.passwordStrengthHint')}</p>
    </>
  )
}

export default PasswordInputWithStrength
