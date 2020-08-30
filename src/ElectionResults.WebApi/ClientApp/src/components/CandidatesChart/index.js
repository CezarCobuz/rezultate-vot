import React from "react";
import { ChartBar } from "./ChartBar";
import { CountiesSelect } from "./CountiesSelect";
import { FormGroup, Col, Button, Media, Label, Container } from "reactstrap";
import code4RoGrey from '../../images/code4RoGrey.svg';
import ElectionPicker from '../../services/electionPicker';
import { getElectionResultsUrl } from '../../services/apiService';
import { useTranslation } from "react-i18next";

let currentSelection = '';
let totalCountedVotes = 0;
let percentageCounted = 0;
let canceledVotes = 0;
let queryUrl = '';

export const ChartContainer = () => {
    const [showAll, toggleShowAll] = React.useState(false);
    const [candidates, setCandidates] = React.useState(null);
    const [counties, setCounties] = React.useState(null);
    const [voterTurnout, setVoterTurnout] = React.useState(null);
    const [displayQuestion, setDisplayQuestion] = React.useState(true);

    const { t } = useTranslation();
    React.useEffect(() => {

        // TODO: Move this in a separate folder and just call here for clearer code
        const fetchServerData = async () => {
            try {
                if (!queryUrl) {
                    queryUrl = getElectionResultsUrl(ElectionPicker.getSelection());
                }
                console.log("Loading results component for " + ElectionPicker.getSelection());
                fetch(queryUrl)
                    .then(data => data.json())
                    .then(data => {
                        if (!data.candidates) {
                            return;
                        }
                        if (data.candidates.length <= 5) {
                            toggleShowAll(true);
                        }
                        data.candidates.forEach((c) => {
                            if (data.candidates.length <= 2)
                                c.displayPercentage = c.percentage * 0.8 - 5;
                            else
                                c.displayPercentage = c.percentage * 0.8;
                            if (c.percentage > 90) {
                                c.displayPercentage = c.percentage * 0.6;
                            }
                        });
                        setCandidates(data.candidates);
                        totalCountedVotes = data.totalCountedVotes + data.canceledVotes;
                        percentageCounted = data.percentageCounted;
                        canceledVotes = data.canceledVotes;
                        setVoterTurnout(data);
                        if (currentSelection) {
                            return;
                        }
                        const total = { label: t('total'), id: "" };
                        const national = { label: t('national'), id: "national" };
                        const diaspora = { label: t('diaspora'), id: "diaspora" };
                        const mail = { label: t('mail'), id: "mail" };
                        data.counties.unshift(total, diaspora, national, mail);
                        setCounties(data.counties);
                    });
            } catch (e) {
                console.log(e);
            }
        };
        const onIdle = () => {
            console.log('results component is idle');
            clearInterval(timer);
            timer = null;
        }
        const onActive = () => {
            console.log('results component is active');
            fetchServerData();
            timer = setInterval(() => fetchServerData(), 1000 * 30);
        }
        const onSelectedElectionsChanged = () => {
            toggleShowAll(false);
            queryUrl = getElectionResultsUrl(ElectionPicker.getSelection());
            fetchServerData();
        }
        window.addEventListener("selectedElectionsChanged", onSelectedElectionsChanged);
        window.addEventListener("onIdle", onIdle);
        window.addEventListener("onActive", onActive);
        fetchServerData();
        let timer = setInterval(() => fetchServerData(), 1000 * 30);
        return () => {
            console.log("cleaned up");
            clearInterval(timer);
            timer = null;
            window.removeEventListener("onIdle", onIdle);
            window.removeEventListener("onActive", onActive);
        };
    }, []);

    const selectionChanged = value => {
        currentSelection = value.id;
        queryUrl = getElectionResultsUrl(ElectionPicker.getSelection(), value.id, value.countyName);
        fetch(queryUrl)
            .then(data => data.json())
            .then(data => {
                console.log(data);

                if (!data.candidates) {
                    return;
                }
                if (data.candidates.length <= 5) {
                    toggleShowAll(true);
                }
                data.candidates.forEach((c) => {
                    if (data.candidates.length <= 2)
                        c.displayPercentage = c.percentage * 0.8 - 5;
                    else
                        c.displayPercentage = c.percentage * 0.8;
                    if (c.percentage > 90) {
                        c.displayPercentage = c.percentage * 0.6;
                    }
                });
                setCandidates(data.candidates);
                totalCountedVotes = data.totalCountedVotes + data.canceledVotes;
                percentageCounted = data.percentageCounted;
                canceledVotes = data.canceledVotes;
                setVoterTurnout(data);
            })
    };

    const cancelledVotes = canceledVotes.toLocaleString(undefined, {
      maximumFractionDigits: 2
    });

    return (
        <div>
            {candidates && voterTurnout ? (
                <div>
                    <div sm={12} className={"votes-container"}>
                        <p className={"container-text"}>
                          {
                            t('site_description')
                          }
                        </p>
                        <div sm={3} className={"votes-numbers"}>
                            <h3 className={"votes-title"}>
                              {
                                t('counted_votes')
                              }
                            </h3>
                            <div sm={3} className={"votes-results"}>
                                <p className={"votes-percent"}>
                                    {" "}
                                    {percentageCounted}%
                </p>
                                <p className={"votes-text"}>
                                    {" "}
                                    {totalCountedVotes.toLocaleString(undefined, {
                                        maximumFractionDigits: 2
                                    })}
                                </p>
                            </div>
                        </div>
                        <p className={"votes-text"} style={{ marginBottom: '0px', textAlign: 'center' }}>
                            {" "}
                          {
                            t('cancelled_votes', { cancelledVotes })
                          }
                        </p>
                    </div>

                    <FormGroup row>
                        <Col sm={3}>
                            <CountiesSelect counties={counties} onSelect={selectionChanged} />
                        </Col>
                    </FormGroup>
                    {(showAll ? candidates : candidates.slice(0, 5)).map(candidate => (
                        <ChartBar
                            key={candidate.id}
                            percent={candidate.percentage}
                            displayPercentage={candidate.displayPercentage}
                            imgUrl={candidate.imageUrl}
                            candidateName={candidate.name}
                            votesNumber={candidate.votes}
                        />
                    ))}
                    {!showAll ? (
                        <div className={"show-all-container"} sm={3}>
                            <Button className={"show-all-btn"} onClick={toggleShowAll}>
                              {
                                t('display_all_candidates')
                              }
                            </Button>
                        </div>
                    ) : null}
                </div>
            ) : (
                    <div className={"default-container"}>
                        <div className={"votes-container"}>
                            <p className={"container-text"}>
                              {
                                t('site_description')
                              }
                            </p>
                        </div>
                        {
                            displayQuestion ? <div className={"question"}>
                                <p className={"question-text"}>
                                  {
                                    t('teaser_question')
                                  }
                                </p>
                            </div> : ""
                        }
                    </div>
                )}
            {
                displayQuestion ? "" :
                    <div style={{ display: 'flex', marginBottom: '50px' }}>
                        <p style={{ position: 'relative', display: 'inline' }} className="becro">{ t('data_source') } <a href="https://prezenta.bec.ro" target="_blank" rel="noopener noreferrer">prezenta.bec.ro</a></p>
                        <Container style={{ display: 'flex', alignItems: 'left', justifyContent: 'flex-end', padding: '0px' }}>
                            <Label style={{ lineHeight: '18px' }} className="info-label">{ t('developed_by') }</Label>
                            <Media src={code4RoGrey} />
                        </Container>
                    </div>
            }
        </div>
    );
};
