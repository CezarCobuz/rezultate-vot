import React, { Suspense } from "react";
import { useTranslation } from "react-i18next";

import { AdminPanel } from "../AdminPanel/AdminPanel";

export const AdminPage = () => {
  const { t } = useTranslation();
  return (
    <Suspense fallback={"Loading..."}>
      <div className="static-container">
        <h2>{t("admin_page")}</h2>
        <AdminPanel />
      </div>
    </Suspense>
  );
};
