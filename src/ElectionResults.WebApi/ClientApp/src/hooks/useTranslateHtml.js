import { useCallback } from "react";
import { renderHtml } from "../services/renderHtml";
import { useTranslation } from "react-i18next";

/**
 * @param {string} namespace
 * @param {string} key
 */
export function useTranslateHtml(namespace, key) {
  const { i18n } = useTranslation();

  const html = i18n.getResource(i18n.language, namespace, key);
  const translate = useCallback(() => {

    return renderHtml(html);
  }, [html]);

  return { translate };
}
