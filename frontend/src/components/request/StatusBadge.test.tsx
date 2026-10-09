import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import StatusBadge from './StatusBadge'

describe('StatusBadge', () => {
  it.each(['Requested', 'Accepted', 'Verified', 'Completed', 'Cancelled'] as const)(
    'shows the %s status label',
    (status) => {
      render(<StatusBadge status={status} />)
      expect(screen.getByText(status)).toBeTruthy()
    },
  )
})