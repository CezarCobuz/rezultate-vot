import React, { useState, useEffect } from 'react';
import { useTranslation } from "react-i18next";
import { Label, Input } from 'reactstrap';
import { getElectionConfigUrl } from '../../services/apiService';

const AdminPanel = () => {
  // const API_URL = '/api/settings/election-config';
  const [config, setConfig] = useState({ Files: [], Candidates: [] });

  useEffect(() => {
    fetch(getElectionConfigUrl())
      .then(data => data.json())
      .then(data => setConfig(data));
  }, []);

  const save = () => {
    fetch(getElectionConfigUrl(), {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(config),
    });
  }

  const addFile = () => {
    const emptyFile = { URL: '', Name: '', Active: false, ResultsLocation: 0, ResultsType: 0 };
    setConfig({ ...config, Files: config.Files.concat(emptyFile) });
  }

  const removeFile = (index) => {
    const filesCopy = Array.from(config.Files);
    filesCopy.splice(index, 1);
    setConfig({ ...config, Files: filesCopy });
  }

  const handleFileChange = (event, index) => {
    const numberFields = ['ResultsType', 'ResultsLocation'];
    const filesCopy = Array.from(config.Files);
    filesCopy[index][event.target.name] = numberFields.includes(event.target.name)
      ? parseInt(event.target.value, 10)
      : event.target.value;
    setConfig({ ...config, Files: filesCopy });
  }

  const addCandidate = () => {
    const emptyCandidate = { Name: '', ImageUrl: '', CsvId: '' };
    setConfig({ ...config, Candidates: config.Candidates.concat(emptyCandidate) });
  }

  const removeCandidate = (index) => {
    const candidatesCopy = Array.from(config.Candidates);
    candidatesCopy.splice(index, 1);
    setConfig({ ...config, Candidates: candidatesCopy });
  }

  const handleCandidateChange = (event, index) => {
    const candidatesCopy = Array.from(config.Candidates);
    candidatesCopy[index][event.target.name] = event.target.value;
    setConfig({ ...config, Candidates: candidatesCopy });
  }

  const { t } = useTranslation();

  return (
    <div>
      <form>
        <div>
          {config.Files.map((file, index) => {
            return (
              <div key={index} style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-end' }}>
                <div className="form-group">
                  <Label for="URL">{t("bec_url")}</Label>
                  <Input type="text" name="URL" placeholder={t("bec_url")} bsSize="sm"
                    value={file.URL} onChange={(event) => handleFileChange(event, index)} />
                </div>
                <div className="form-group">
                  <Label for="ResultsType">{t("results_type")}</Label>
                  <Input type="select" name="ResultsType" bsSize="sm"
                    value={file.ResultsType} onChange={(event) => handleFileChange(event, index)}>
                    <option value={0}>{t("provisional")}</option>
                    <option value={1}>{t("partial")}</option>
                    <option value={2}>{t("final")}</option>
                    <option value={3}>{t("turnout")}</option>
                    <option value={4}>{t("vote_monitoring")}</option>
                  </Input>
                </div>
                <div className="form-group">
                  <Label for="ResultsLocation">{t("location")}</Label>
                  <Input type="select" name="ResultsLocation" bsSize="sm"
                    value={file.ResultsLocation} onChange={(event) => handleFileChange(event, index)}>
                    <option value={0}>{t("romania")}</option>
                    <option value={1}>{t("diaspora")}</option>
                    <option value={2}>{t("all")}</option>
                  </Input>
                </div>
                <div className="form-group">
                  <Label for="Active" check className="position-relative">
                    <Input type="checkbox" name="Active" value={file.Active}
                      style={{ width: '1em', height: '1em' }} />
                    {t("is_active")}
                  </Label>
                </div>
                <div className="form-group">
                  <button type="button" className="btn btn-sm btn-secondary" onClick={() => removeFile(index)}>{t("remove")}</button>
                </div>
              </div>
            )
          })}
          <button type="button" className="btn btn-sm btn-secondary mb-3" onClick={addFile}>{t("add_new_url")}</button>
        </div>
        <div>
          {config.Candidates.map((candidate, index) => {
            return (
              <div key={index} style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-end' }}>
                <div className="form-group">
                  <Label for="Name">{t("name")}</Label>
                  <Input type="text" name="Name" placeholder={t("name")} bsSize="sm"
                    value={candidate.Name} onChange={(event) => handleCandidateChange(event, index)} />
                </div>
                <div className="form-group">
                  <Label for="ImageUrl">{t("image_url")}</Label>
                  <Input type="text" name="ImageUrl" placeholder={t("image_url")} bsSize="sm"
                    value={candidate.ImageUrl} onChange={(event) => handleCandidateChange(event, index)} />
                </div>
                <div className="form-group">
                  <Label for="CsvId">{t("csv_id")}</Label>
                  <Input type="text" name="CsvId" placeholder={t("csv_id")} bsSize="sm"
                    value={candidate.CsvId} onChange={(event) => handleCandidateChange(event, index)} />
                </div>
                <div className="form-group">
                  <button type="button" className="btn btn-sm btn-secondary" onClick={() => removeCandidate(index)}>{t("remove")}</button>
                </div>
              </div>
            )
          })}
          <button type="button" className="btn btn-sm btn-secondary mb-3" onClick={addCandidate}>{t("add_new_candidate")}</button>
        </div>
      </form>
      <button type="button" className="btn btn-sm btn-success mb-3" onClick={save}>{t("save")}</button>
    </div>
  )
}

export { AdminPanel };
