import React from "react";
import "./style.css";
import ElectionPicker from '../../services/electionPicker';
import { getVoteMonitoringUrl } from '../../services/apiService';
import { useTranslation } from "react-i18next";

const NumberArea = ({ bigNumber, text }) => (
    <div className={"vote-monitoring-area"}>
        <p className={"area-number"}>{bigNumber}</p>
        <p className={"area-text"}>{text}</p>
    </div>
);
export const VoteMonitoring = () => {

    const [voteMonitoringData, setVoteMonitoringData] = React.useState(null);
    React.useEffect(() => {
        const fetchServerData = async () => {
            try {
                console.log("Loaded vote monitoring for " + ElectionPicker.getSelection());
                /*fetch(getVoteMonitoringUrl(ElectionPicker.getSelection()))
                    .then(data => data.json())
                    .then(data => setVoteMonitoringData(data.statistics));*/
            } catch (e) {
                console.log(e);
            }
        };

        const onIdle = () => {
            clearInterval(timer);
            timer = null;
        }
        const onActive = () => {
            timer = setInterval(() => fetchServerData(), 1000 * 60 * 10);
        }
        window.addEventListener("onIdle", onIdle);
        window.addEventListener("onActive", onActive);
        fetchServerData();
        let timer = setInterval(() => fetchServerData(), 1000 * 60 * 10);
        return () => {
            console.log("cleaned up vote monitoring component");
            clearInterval(timer);
            timer = null;
            window.removeEventListener("onIdle", onIdle);
            window.removeEventListener("onActive", onActive);
        };
    }, []);

    const { t } = useTranslation();
    if (!voteMonitoringData) {
        return null;
    } else {
        const messagesNumber = voteMonitoringData[0].value;
        const messagesWithProblems = voteMonitoringData[5].value;
        const percent = (messagesWithProblems / messagesNumber) * 100;
        return (
            <div className={"full-width-container"}>
                <div className={"vote-monitoring-container"}>
                    <div className={"vote-monitoring-title"}>
                        <h1>
                            {
                                t('vote_monitoring')
                            }
                        </h1>
                    </div>
                    <div className="vote-monitoring-message">
                        {
                            t('vote_monitoring_message')
                        }
                    </div>
                    <div className={"vote-monitoring-numbers"}>
                        {NumberArea({
                            bigNumber: voteMonitoringData[1].value,
                            text: t('number_of_visited_polling_stations')
                        })}
                        {NumberArea({
                            bigNumber: voteMonitoringData[2].value,
                            text: t('number_of_visited_counties')
                        })}
                        {NumberArea({
                            bigNumber: voteMonitoringData[4].value,
                            text: t('number_of_logged_in_observers')
                        })}
                    </div>
                    <div className={"vote-monitoring-info"}>
                        <div className={"info-legend"}>
                            <div className={"legend-container space"}>
                                <div className={ "square left" }/>
                                <p>
                                    {
                                        t('observer_messages')
                                    }
                                </p>
                            </div>
                            <div className={"legend-container"}>
                                <div className={ "square right" }/>
                                <p>
                                    {
                                        t('observed_issues')
                                    }
                                </p>
                            </div>
                        </div>
                        <div className={"info-legend bars"}>
                            <div className={"parent-bar"}>
                                <p style={{ paddingLeft: `${percent + 9}%` }}>
                                    {voteMonitoringData[0].value}
                                </p>
                            </div>
                            <div
                                style={{
                                    width: `${percent}%`,
                                    minWidth: '8%',
                                    fontSize: '20px',
                                    justifyContent: 'center'
                                }}
                                className={"child-bar"}
                            >
                                <p>{voteMonitoringData[5].value}</p>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        );
    }
};
