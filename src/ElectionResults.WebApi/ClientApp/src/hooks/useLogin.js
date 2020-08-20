
import { useEffect, useState } from "react";

/**
 * @param {boolean} deferred
 * @returns {{isLoading: boolean, response: null, error: null}}
 */
export const useLogin = (deferred) => {
  const [state, setState] = useState(
    {
      isLoading: false,
      response: null,
      error: null
    }
  );

  useEffect(() => {
    if (deferred) {
      setState({
        ...state,
        isLoading: true
      });
    }

  }, [deferred]);

  return { ...state };
}
